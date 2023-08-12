using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Client;

public static class HubConnectionExtensions
{
	public static async IAsyncEnumerable<T> StreamAsync<T>(
		this HubConnection connection, 
		string methodName,
		params object[] arguments)
	{
		using var cancellationSource = new CancellationTokenSource();
		try
		{
			var channel =
				await connection.StreamAsChannelCoreAsync<T>(methodName, arguments, cancellationSource.Token);
			while (await channel.WaitToReadAsync(cancellationSource.Token))
			{
				while (channel.TryRead(out var ret) && !cancellationSource.Token.IsCancellationRequested)
					yield return ret;
			}
		}
		finally
		{
			if (!cancellationSource.IsCancellationRequested)
				cancellationSource.Cancel();
		}
	}

	public static StreamInvoker<T> GetStreamInvoker<T>(this HubConnection connection, [CallerMemberName] string methodName = null!)
	{
		return new StreamInvoker<T>(connection, methodName);
	}

	public readonly record struct StreamInvoker<T>(HubConnection Connection, string MethodName)
	{
		public IAsyncEnumerable<T> InvokeAsync(params object[] arguments) =>
			Connection.StreamAsync<T>(MethodName, arguments);
	}
}
