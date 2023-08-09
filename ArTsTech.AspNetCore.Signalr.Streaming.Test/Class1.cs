using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;
using NUnit.Framework;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Test
{
	[TestFixture]
	public class Class1
	{
		[Test]
		public async Task DoStuff()
		{
			using var host = new WebHostBuilder()
				.UseStartup<Startup>()
				.UseKestrel()
				.Build();
			await host.StartAsync();

			var server = host.ServerFeatures.Get<IServerAddressesFeature>();
			var address = server.Addresses.First()!;
			
			var client = new HubConnectionBuilder()
				.WithUrl($"{address}/signalr/count")
				.Build();

			var channel = await client.StreamAsChannelAsync<int>("Count");
			while (await channel.WaitToReadAsync())
			{
				while (channel.TryRead(out var ret))
				{
					Console.WriteLine(ret);
				}
			}
		}
	}
}