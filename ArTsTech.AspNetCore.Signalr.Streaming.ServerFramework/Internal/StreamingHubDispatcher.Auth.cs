using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Internal;

partial class StreamingHubDispatcher<THub>
{
	protected ValueTask<bool> IsHubMethodAuthorized(IServiceProvider provider, ClaimsPrincipal principal, IReadOnlyList<IAuthorizeData> policies)
	{
		// If there are no policies we don't need to run auth
		if (!policies.Any())
		{
			return new(true);
		}

		return IsHubMethodAuthorizedSlow(provider, principal, policies);
	}

	private static async ValueTask<bool> IsHubMethodAuthorizedSlow(IServiceProvider provider, ClaimsPrincipal principal, IReadOnlyList<IAuthorizeData> policies)
	{
		var authService = provider.GetRequiredService<IAuthorizationService>();
		var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

		var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
		// AuthorizationPolicy.CombineAsync only returns null if there are no policies and we check that above
		Debug.Assert(authorizePolicy != null);

		var authorizationResult = await authService.AuthorizeAsync(principal, authorizePolicy);
		// Only check authorization success, challenge or forbid wouldn't make sense from a hub method invocation
		return authorizationResult.Succeeded;
	}
	
	protected async Task SendInvocationError(string invocationId, HubConnectionContext connection, string errorMessage)
	{
		if (string.IsNullOrEmpty(invocationId))
		{
			return;
		}

		await connection.WriteAsync(CompletionMessage.WithError(invocationId, errorMessage));
	}
}