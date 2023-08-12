using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace ArTsTech.AspNetCore.Signalr.Streaming;

public static class StreamingSignalR
{
	public static ISignalRServerBuilder AddStreamingSignalRCore(this IServiceCollection services)
	{
		return services
			.AddSingleton(typeof(HubDispatcher<>), typeof(Internal.StreamingHubDispatcher<>))
			.AddSignalR();
	}
}