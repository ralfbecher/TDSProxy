using System.Collections.Generic;

namespace TDSProxy.Configuration
{
	public class TdsProxySection
	{
		public List<ListenerElement> Listeners { get; set; } = new List<ListenerElement>();
	}
}
