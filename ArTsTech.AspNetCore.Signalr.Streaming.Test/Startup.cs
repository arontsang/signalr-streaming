using ArTsTech.AspNetCore.Signalr.Streaming.Test.Signalr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Test;

public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services
			.AddStreamingSignalRCore();
	}

	public void Configure(IApplicationBuilder app, IHostingEnvironment env)
	{
		app
			.UseWebSockets()
			.UseSignalR(routes =>
			{
				routes.MapHub<CountHub>(CountHub.HubPath);
			});
	}
}