using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.SignalR;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Internal;

public static class HubConnectionContextExtensions
{
	private static readonly Func<HubConnectionContext, ConcurrentDictionary<string, CancellationTokenSource>> _getter;

	static HubConnectionContextExtensions()
	{
		var input = Expression.Parameter(typeof(HubConnectionContext));
		var property = typeof(HubConnectionContext)
			.GetProperty("ActiveRequestCancellationSources", BindingFlags.Instance | BindingFlags.NonPublic)!;

		_getter = Expression.Lambda<Func<HubConnectionContext, ConcurrentDictionary<string, CancellationTokenSource>>>(
			Expression.Property(input, property),
			input).Compile();
	}

	public static ConcurrentDictionary<string, CancellationTokenSource> GetActiveRequestCancellationSources(
		this HubConnectionContext connection)
	{
		return _getter(connection);
	}
	
	public static bool TryRegisterRequestCancellationSource(
		this HubConnectionContext connection,
		string invocationId,
		CancellationTokenSource cancellationTokenSource)
	{
		var registry = _getter(connection);
		return registry.TryAdd(invocationId, cancellationTokenSource);
	}
	
	public static bool TryUnregisterRequestCancellationSource(
		this HubConnectionContext connection,
		string invocationId)
	{
		var registry = _getter(connection);
		return registry.TryRemove(invocationId, out _);
	}
}