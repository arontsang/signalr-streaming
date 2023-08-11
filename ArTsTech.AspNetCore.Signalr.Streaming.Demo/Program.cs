using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ArTsTech.AspNetCore.Signalr.Streaming.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;

using var host = new WebHostBuilder()
	.UseStartup<Startup>()
	.UseKestrel(options =>
	{
		options.ListenLocalhost(3000);
	})
	.Build();
await host.StartAsync();

var server = host.ServerFeatures.Get<IServerAddressesFeature>();
var address = server.Addresses.First()!;

var client = new HubConnectionBuilder()
	.WithUrl($"{address}/signalr/count")
	.Build();
await client.StartAsync();
var channel = client.Observe<int>("CountAsync");

await foreach (var item in channel.ToAsyncEnumerable().Take(5))
{
	Console.WriteLine(item);
}

Console.WriteLine("Fin");
await host.StopAsync();

static class Foo
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
	
	public static IObservable<T> Observe<T>(
		this HubConnection connection, 
		string methodName,
		params object[] arguments)
	{
		return Observable.Create<T>(async (observer, cancel) =>
		{
			try
			{
				var channel = await connection.StreamAsChannelCoreAsync<T>(methodName, arguments, cancel);
				while (await channel.WaitToReadAsync(cancel))
				{
					while (channel.TryRead(out var ret) && !cancel.IsCancellationRequested)
						observer.OnNext(ret);
				}
				observer.OnCompleted();
			}
			catch (Exception ex)
			{
				observer.OnError(ex);
			}
		});
	}
}