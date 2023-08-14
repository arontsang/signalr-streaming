using System;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Internal;

partial class StreamingHubDispatcher<THub>
{
	private static class Log
	{
		private static readonly Action<ILogger, StreamInvocationMessage, Exception?> _receivedStreamHubInvocation =
			LoggerMessage.Define<StreamInvocationMessage>(LogLevel.Debug, new EventId(12, "ReceivedStreamHubInvocation"), "Received stream hub invocation: {InvocationMessage}.");
		private static readonly Action<ILogger, string, Exception?> _hubMethodNotAuthorized =
			LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "HubMethodNotAuthorized"), "Failed to invoke '{HubMethod}' because user is unauthorized.");
		public static void ReceivedStreamHubInvocation(ILogger logger, StreamInvocationMessage invocationMessage)
		{
			_receivedStreamHubInvocation(logger, invocationMessage, null);
		}
		
		public static void HubMethodNotAuthorized(ILogger logger, string hubMethod)
		{
			_hubMethodNotAuthorized(logger, hubMethod, null);
		}
	}
}