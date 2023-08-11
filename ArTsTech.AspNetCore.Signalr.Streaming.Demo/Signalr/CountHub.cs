using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Test.Signalr;

public class CountHub : Hub
{
	public IAsyncEnumerable<int> CountAsync()
	{
		async IAsyncEnumerator<int> Impl(CancellationToken cancellationToken)
		{
			for (var i = 0; true; i++)
			{
				await Task.Delay(100, cancellationToken);
				yield return i;
			}
		}

		return AsyncEnumerable.Create(Impl);
	}
	
	public Task<int> Foo()
	{
		

		return Task.FromResult(42);
	}
}