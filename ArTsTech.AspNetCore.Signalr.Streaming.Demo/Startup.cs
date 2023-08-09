using ArTsTech.AspNetCore.Signalr.Streaming.Test.Signalr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Demo;

public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services
			//.AddWebSockets(() => { })
			.AddStreamingSignalRCore();
	}

	public void Configure(IApplicationBuilder app, IHostingEnvironment env)
	{
		app
			//.UseWebSockets()
			.UseSignalR(routes =>
			{
				routes.MapHub<CountHub>("/signalr/count");
			});
	}
}