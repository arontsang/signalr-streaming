using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Internal;

public partial class StreamingHubDispatcher<THub> : DefaultHubDispatcher<THub> where THub : Hub
{
	private static readonly IReadOnlyDictionary<string, IStreamingMethodDescription<THub>> HubMethods;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly IHubContext<THub> _hubContext;
	private readonly ILogger<StreamingHubDispatcher<THub>> _logger;

	static StreamingHubDispatcher()
	{
		HubMethods = DiscoverHubMethods();
	}

	public StreamingHubDispatcher(
		IServiceScopeFactory serviceScopeFactory,
		IHubContext<THub> hubContext,
		IOptions<HubOptions<THub>> hubOptions, 
		IOptions<HubOptions> globalHubOptions, 
		ILogger<StreamingHubDispatcher<THub>> logger) : base(serviceScopeFactory, hubContext, hubOptions, globalHubOptions, logger)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_hubContext = hubContext;
		_logger = logger;
	}

	public override Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
	{
		if (hubMessage is StreamInvocationMessage streamInvocationMessage &&
		    HubMethods.TryGetValue(streamInvocationMessage.Target, out var streamingMethodDescription))
		{
			Log.ReceivedStreamHubInvocation(_logger, streamInvocationMessage);
			return ProcessStreamingInvocation(connection, streamInvocationMessage, streamingMethodDescription);
		}
		
		return base.DispatchMessageAsync(connection, hubMessage);
	}
    
	private Task ProcessStreamingInvocation(
		HubConnectionContext connection,
		StreamInvocationMessage hubMethodInvocationMessage,
		IStreamingMethodDescription<THub> descriptor)
	{
		var arguments = new object[descriptor.OriginalParameterTypes.Count];
		var cts = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted);

		CheckArgumentCompatability(arguments, descriptor);

		_ = Task.Run(async () =>
		{
			using var scope = _serviceScopeFactory.CreateScope();
			if (false == await IsHubMethodAuthorized(scope.ServiceProvider, connection.User, descriptor.Policies))
			{
				Log.HubMethodNotAuthorized(_logger, hubMethodInvocationMessage.Target);
				await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
					$"Failed to invoke '{hubMethodInvocationMessage.Target}' because user is unauthorized");
				return;
			}
			
			var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
			var hub = hubActivator.Create();
			InitializeHub(hub, connection);
			try
			{
				if (!cts.IsCancellationRequested)
				{
					_ = connection.TryRegisterRequestCancellationSource(hubMethodInvocationMessage.InvocationId, cts);

					await descriptor.InvokeStream(
						hub,
						_logger,
						hubMethodInvocationMessage.InvocationId,
						connection,
						arguments,
						cts.Token);
				}
			}
			finally
			{
				_ = connection.TryUnregisterRequestCancellationSource(hubMethodInvocationMessage.InvocationId);
				hubActivator.Release(hub);
				cts.Dispose();
			}
		}, CancellationToken.None);
		
		return Task.CompletedTask;
	}

	private void CheckArgumentCompatability(
		object[] arguments, 
		IStreamingMethodDescription<THub> descriptor)
	{
		// TODO: Check that descriptor.OriginalArguments match arguments
	}

	private void InitializeHub(THub hub, HubConnectionContext connection)
	{
		hub.Clients = new HubCallerClients(_hubContext.Clients, connection.ConnectionId);
		hub.Context = new DefaultHubCallerContext(connection);
		hub.Groups = _hubContext.Groups;
	}
	
	private static IReadOnlyDictionary<string, IStreamingMethodDescription<THub>> DiscoverHubMethods()
	{
		var hubType = typeof(THub);

		var ret = new Dictionary<string, IStreamingMethodDescription<THub>>(StringComparer.Ordinal);
		foreach (var methodInfo in HubReflectionHelper.GetHubMethods(hubType))
		{
			if (StreamingMethodDescription<THub>.Build(methodInfo) is {} methodDescription)
			{
				var methodName =
					methodInfo.GetCustomAttribute<HubMethodNameAttribute>()?.Name ??
					methodInfo.Name;

				ret[methodName] = methodDescription;
			}
		}

		return ret;
	}
}