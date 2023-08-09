using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace ArTsTech.AspNetCore.Signalr.Streaming;

public static class StreamingSignalR
{
	public static IServiceCollection AddStreamingSignalRCore(this IServiceCollection services)
	{
		return services
			.AddSingleton(typeof(HubDispatcher<>), typeof(Internal.StreamingHubDispatcher<>))
			.AddSignalR()
			.Services;
	}
}