using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ArTsTech.AspNetCore.Signalr.Streaming.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;

using var host = new WebHostBuilder()
	.UseStartup<Startup>()
	.UseKestrel()
	.Build();
await host.StartAsync();

var server = host.ServerFeatures.Get<IServerAddressesFeature>();
var address = server.Addresses.First()!;

await Task.Delay(Timeout.Infinite);
// var client = new HubConnectionBuilder()
// 	.WithUrl($"{address}/signalr/count")
// 	.Build();
// await client.StartAsync();
// using var cancellationTokenSource = new CancellationTokenSource();
// var channel = await client.StreamAsChannelAsync<int>("CountAsync", cancellationTokenSource.Token);
// while (await channel.WaitToReadAsync())
// {
// 	while (channel.TryRead(out var ret))
// 	{
// 		Console.WriteLine(ret);
// 		if (ret == 3)
// 			cancellationTokenSource.Cancel();
// 	}
// }
//
// Console.WriteLine("Fin");