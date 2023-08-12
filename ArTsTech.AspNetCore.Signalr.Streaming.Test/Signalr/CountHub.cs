using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ArTsTech.AspNetCore.Signalr.Streaming.Test.Signalr;

public class CountHub : Hub<ICallback>
{
	public const string HubPath = "/signalr/count";
	
	public async IAsyncEnumerable<int> CountAsync()
	{
		try
		{
			foreach (var i in Enumerable.Range(0, 10))
			{
				yield return i;
				await Task.Delay(0);
			}
		}
		finally
		{
			await Clients.Caller.CountAsyncStopped();
		}
	}
}

public interface ICallback
{
	Task CountAsyncStopped();
}