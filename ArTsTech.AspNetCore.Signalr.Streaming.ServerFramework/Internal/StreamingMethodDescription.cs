using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Internal;

public static class StreamingMethodDescription<THub>
{
	public static IStreamingMethodDescription<THub>? Build(MethodInfo methodInfo)
	{
		if (GetStreamingItemType(methodInfo.ReturnType) is not { } streamingItemType)
			return default;

		var (itemType, streamType) = streamingItemType;
		var streamingType = typeof(MethodDescription<>).MakeGenericType(typeof(THub), itemType);
		return (IStreamingMethodDescription<THub>)Activator.CreateInstance(
			streamingType,
			methodInfo,
			streamType == StreamType.Observable);
	}

	private static (Type ItemType, StreamType StreamType)? GetStreamingItemType(Type type)
	{
		if (type is { IsInterface: true, IsGenericType: true }
		    && type.GetGenericTypeDefinition() is { } genericTypeDefinition)
		{
			if (genericTypeDefinition == typeof(IAsyncEnumerable<>))
				return (type.GetGenericArguments()[0], StreamType.AsyncEnumerable);
			if (genericTypeDefinition == typeof(IObservable<>))
				return (type.GetGenericArguments()[0], StreamType.Observable);
		}

		return null;
	}

	private enum StreamType
	{
		AsyncEnumerable,
		Observable,
	}

	private class MethodDescription<TItem> : IStreamingMethodDescription<THub>
	{
		private static readonly MethodInfo ConvertToAsyncEnumerableMethodInfo = typeof(AsyncEnumerable)
			.GetMethod(nameof(AsyncEnumerable.ToAsyncEnumerable), BindingFlags.Public | BindingFlags.Static)!
			.MakeGenericMethod(typeof(TItem));

		private delegate IAsyncEnumerable<TItem> Invoker(THub hub, object[] arguments);

		public MethodDescription(MethodInfo methodInfo, bool isObservable)
		{
			OriginalParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToList();
			MethodInfo = methodInfo;
			_invoker = BuildInvoker(methodInfo, isObservable);
		}

		public async Task InvokeStream(THub hub,
			ILogger logger,
			string invocationId,
			HubConnectionContext connection,
			object[] arguments,
			CancellationToken cancellationToken)
		{
			try
			{
				await foreach (var item in _invoker(hub, arguments).WithCancellation(cancellationToken))
				{
					await connection.WriteAsync(new StreamItemMessage(invocationId, item), cancellationToken);
				}

				await connection.WriteAsync(new CompletionMessage(invocationId, null, null, false), cancellationToken);
			}
			catch (OperationCanceledException)
			{
				logger.LogInformation("Streaming results cancelled by user");
			}
			catch (Exception ex)
			{
				var error = BuildErrorMessage("An error occurred on the server while streaming results.", ex, false);
				logger.LogError(ex, "An error occurred on the server while streaming results");
				await connection.WriteAsync(CompletionMessage.WithError(invocationId, error), cancellationToken);
			}
		}

		private static Invoker BuildInvoker(MethodInfo methodInfo, bool isObservable)
		{
			var arguments = new List<Expression>();
			var hub = Expression.Parameter(typeof(THub));
			var input = Expression.Parameter(typeof(object[]));

			foreach (var parameterInfo in methodInfo.GetParameters())
			{
				var index = arguments.Count;
				var arg = Expression.ArrayIndex(input, Expression.Constant(index));
				arguments.Add(Expression.Convert(arg, parameterInfo.ParameterType));
			}

			var invocation = Expression.Call(hub, methodInfo, arguments);

			if (isObservable)
				invocation = Expression.Call(ConvertToAsyncEnumerableMethodInfo, invocation);

			var lambda = Expression.Lambda<Invoker>(
				invocation,
				hub,
				input);

			return lambda.Compile();
		}

		private readonly Invoker _invoker;

		public IReadOnlyList<Type> OriginalParameterTypes { get; }
		public MethodInfo MethodInfo { get; }

		private static string BuildErrorMessage(string message, Exception exception, bool includeExceptionDetails)
		{
			if (exception is HubException || includeExceptionDetails)
			{
				return $"{message} {exception.GetType().Name}: {exception.Message}";
			}

			return message;
		}
	}
}

public interface IStreamingMethodDescription<in THub>
{
	Task InvokeStream(THub hub, ILogger logger, string invocationId, HubConnectionContext hubConnectionContext,
		object[] arguments, CancellationToken cancellationToken);

	IReadOnlyList<Type> OriginalParameterTypes { get; }
	MethodInfo MethodInfo { get; }
}