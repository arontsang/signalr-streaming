using System;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Internal;

partial class StreamingHubDispatcher<THub>
{
	private static class Log
	{
		private static readonly Action<ILogger, StreamInvocationMessage, Exception> _receivedStreamHubInvocation =
			LoggerMessage.Define<StreamInvocationMessage>(LogLevel.Debug, new EventId(12, "ReceivedStreamHubInvocation"), "Received stream hub invocation: {InvocationMessage}.");

		public static void ReceivedStreamHubInvocation(ILogger logger, StreamInvocationMessage invocationMessage)
		{
			_receivedStreamHubInvocation(logger, invocationMessage, null);
		}
	}
}