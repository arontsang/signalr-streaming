using System;
using System.Collections.Generic;
using System.Diagnostics;
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

	static StreamingHubDispatcher()
	{
		HubMethods = DiscoverHubMethods();
	}

	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly ILogger<StreamingHubDispatcher<THub>> _logger;

	public StreamingHubDispatcher(
		IServiceScopeFactory serviceScopeFactory,
		IHubContext<THub> hubContext, 
		IOptions<HubOptions<THub>> hubOptions, 
		IOptions<HubOptions> globalHubOptions, 
		ILogger<StreamingHubDispatcher<THub>> logger) : base(serviceScopeFactory, hubContext, hubOptions, globalHubOptions, logger)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_logger = logger;
	}

	public override Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
	{
		switch (hubMessage)
		{
			case StreamInvocationMessage streamInvocationMessage when HubMethods.TryGetValue(streamInvocationMessage.Target, out var streamingMethodDescription):
				Log.ReceivedStreamHubInvocation(_logger, streamInvocationMessage);
				return ProcessInvocation(connection, streamInvocationMessage, streamingMethodDescription);
			
			case CancelInvocationMessage cancelInvocationMessage:
				if (connection.GetActiveRequestCancellationSources().TryGetValue(cancelInvocationMessage.InvocationId,
					    out var cancellationTokenSource))
				{
					cancellationTokenSource.Dispose();
				}
				return base.DispatchMessageAsync(connection, hubMessage);
			case StreamInvocationMessage:
			case InvocationBindingFailureMessage:
			case PingMessage:
			case CloseMessage:
			case InvocationMessage:
				return base.DispatchMessageAsync(connection, hubMessage);
			// TODO: Client side streaming
			// case StreamItemMessage streamItem:
			//     return ProcessStreamItem(connection, streamItem);
			// case CompletionMessage completionMessage:
			//     // closes channels, removes from Lookup dict
			//     // user's method can see the channel is complete and begin wrapping up
			//     if (connection.StreamTracker.TryComplete(completionMessage))
			//     {
			//         Log.CompletingStream(_logger, completionMessage);
			//     }
			//     // InvocationId is always required on CompletionMessage, it's nullable because of the base type
			//     else if (_hubLifetimeManager.TryGetReturnType(completionMessage.InvocationId!, out _))
			//     {
			//         return _hubLifetimeManager.SetConnectionResultAsync(connection.ConnectionId, completionMessage);
			//     }
			//     else
			//     {
			//         Log.UnexpectedCompletion(_logger, completionMessage.InvocationId!);
			//     }
			//     break;

			// Other kind of message we weren't expecting
			default:
				Log.UnsupportedMessageReceived(_logger, hubMessage.GetType().FullName!);
				throw new NotSupportedException($"Received unsupported message: {hubMessage}");
		}
	}
    
	private async Task ProcessInvocation(
		HubConnectionContext connection,
		HubMethodInvocationMessage hubMethodInvocationMessage,
		IStreamingMethodDescription<THub> descriptor)
	{
		using var scope = _serviceScopeFactory.CreateScope();
		var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
		var hub = hubActivator.Create();

		try
		{
			var arguments = new object[descriptor.OriginalParameterTypes.Count];

			CancellationTokenSource? cts = null;
			var hubInvocationArgumentPointer = 0;
			for (var parameterPointer = 0; parameterPointer < arguments.Length; parameterPointer++)
			{
				if (hubMethodInvocationMessage.Arguments.Length > hubInvocationArgumentPointer &&
				    hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer].GetType() ==
				    descriptor.OriginalParameterTypes[parameterPointer])
				{
					// The types match so it isn't a synthetic argument, just copy it into the arguments array
					arguments[parameterPointer] = hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer];
					hubInvocationArgumentPointer++;
				}
				else
				{
					// This is the only synthetic argument type we currently support
					if (descriptor.OriginalParameterTypes[parameterPointer] == typeof(CancellationToken))
					{
						cts = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted);
						arguments[parameterPointer] = cts.Token;
					}
					else
					{
						// This should never happen
						Debug.Assert(false,
							$"Failed to bind argument of type '{descriptor.OriginalParameterTypes[parameterPointer].Name}' for hub method '{descriptor.MethodInfo.Name}'.");
					}
				}
			}

			if (cts == null)
			{
				cts = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted);
			}

			using var _ = connection.ConnectionAborted.Register(() =>
			{
				Console.WriteLine("Disconnect");
			});

			//if (cts != null)
				connection.GetActiveRequestCancellationSources().TryAdd(hubMethodInvocationMessage.InvocationId, cts);

			await descriptor.InvokeStream(hub, hubMethodInvocationMessage.InvocationId, connection, arguments, cts.Token);
		}
		finally
		{
			hubActivator.Release(hub);
		}
	}
	
	private static IReadOnlyDictionary<string, IStreamingMethodDescription<THub>> DiscoverHubMethods()
	{
		var hubType = typeof(THub);

		var ret = new Dictionary<string, IStreamingMethodDescription<THub>>();
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