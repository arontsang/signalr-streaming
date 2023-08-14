using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ArTsTech.AspNetCore.Signalr.Streaming.Client;
using ArTsTech.AspNetCore.Signalr.Streaming.Test.Signalr;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Nito.AsyncEx;
using NUnit.Framework;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Test
{
	[TestFixture]
	public class Class1
	{
		private readonly IWebHost _host = new WebHostBuilder()
			.UseStartup<Startup>()
			.UseKestrel(options =>
			{
				options.Listen(IPAddress.IPv6Loopback, 0);
			})
			.Build();
		
		[OneTimeSetUp]
		public async Task BuildHost()
		{
			await _host.StartAsync();
		}

		[OneTimeTearDown]
		public async Task StopHost()
		{
			await _host.StopAsync();
			_host.Dispose();
		}

		private string HubUrl
		{
			get
			{
				var server = _host.ServerFeatures.Get<IServerAddressesFeature>();
				var address = server.Addresses.First()!;
				return $"{address}{CountHub.HubPath}";
			}
		}
		
		[Test]
		public async Task Test_Cancellation_Flows_To_Server()
		{
			var client = new HubConnectionBuilder()
				.WithUrl(HubUrl)
				.Build();

			var hasBeenCancelledOnServer = new AsyncAutoResetEvent();
			using var _ = client.On(nameof(ICallback.CountAsyncStopped), hasBeenCancelledOnServer.Set);
			
			await client.StartAsync();

			var counts = await client.StreamAsync<int>(nameof(CountHub.CountAsync)).Take(5).ToListAsync();
			CollectionAssert.AreEqual(Enumerable.Range(0, 5), counts);


			using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(0));
			Assert.DoesNotThrowAsync(async () =>
			{
				await hasBeenCancelledOnServer.WaitAsync(timeout.Token);
			}, "Task not cancelled on server");
			
		}
		
		[Test]
		public async Task Test_Cancellation_End_Of_Stream_Work()
		{
			var client = new HubConnectionBuilder()
				.WithUrl(HubUrl)
				.Build();

			await client.StartAsync();

			var counts = await client.StreamAsync<int>(nameof(CountHub.CountAsync)).ToListAsync();
			CollectionAssert.AreEqual(Enumerable.Range(0, 10), counts);
		}
		
		[Test]
		public async Task Test_Throw_works()
		{
			var client = new HubConnectionBuilder()
				.WithUrl(HubUrl)
				.Build();

			await client.StartAsync();

			Assert.ThrowsAsync<HubException>(async () =>
			{
				await client.StreamAsync<int>(nameof(CountHub.ThrowOnThird)).ToListAsync();
			});
		}
		
		
		[Test]
		public async Task Auth_NotAuth_Works()
		{
			var client = new HubConnectionBuilder()
				.WithUrl(HubUrl)
				.Build();

			await client.StartAsync();

			Assert.ThrowsAsync<HubException>(async () =>
			{
				await client.StreamAsync<int>(nameof(CountHub.NotAuth)).ToListAsync();
			});
		}
		
		[Test]
		public async Task Auth_Success_Works()
		{
			var client = new HubConnectionBuilder()
				.WithUrl(HubUrl)
				.Build();

			await client.StartAsync();

			Assert.DoesNotThrowAsync(async () =>
			{
				await client.StreamAsync<int>(nameof(CountHub.AuthAllowAll)).ToListAsync();
			});
		}
	}
}