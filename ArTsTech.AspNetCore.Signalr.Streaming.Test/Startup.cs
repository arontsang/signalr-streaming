using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArTsTech.AspNetCore.Signalr.Streaming.Test.Signalr;
using Microsoft.AspNetCore.Authorization;
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

		services.AddAuthorization(options =>
		{
			options.AddPolicy("Never", policy => policy.AddRequirements(new StaticAuthRequirement(false)));
			options.AddPolicy("Success", policy => policy.AddRequirements(new StaticAuthRequirement(true)));
		})
			.AddSingleton<IAuthorizationHandler, StaticAuthRequirementHandler>();
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

	private record StaticAuthRequirement(bool Allow) : IAuthorizationRequirement
	{
		
	}

	private class StaticAuthRequirementHandler : AuthorizationHandler<StaticAuthRequirement>
	{
		protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, StaticAuthRequirement requirement)
		{
			if (requirement.Allow)
			{
				context.Succeed(requirement);
				return Task.CompletedTask;
			}
			
			context.Fail();
			return Task.CompletedTask;
		}
	}
}