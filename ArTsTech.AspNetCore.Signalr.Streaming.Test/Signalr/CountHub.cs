using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Test.Signalr;

public class CountHub : Hub
{
	public async IAsyncEnumerable<int> CountAsync()
	{
		foreach (var i in Enumerable.Range(0, 10))
			yield return i;
	}
}