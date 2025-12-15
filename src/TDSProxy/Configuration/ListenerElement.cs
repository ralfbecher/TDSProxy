using System.Collections.Generic;
using System.Net;

namespace TDSProxy.Configuration
{
	public class ListenerElement
	{
		public string Name { get; set; }

		public string BindToAddress { get; set; }

		public ushort ListenOnPort { get; set; }

		public string ForwardToHost { get; set; }

		public ushort ForwardToPort { get; set; }

		public SslCertificateConfig SslCertificate { get; set; }

		public List<AuthenticatorElement> Authenticators { get; set; } = new List<AuthenticatorElement>();

		public IPAddress GetBindToAddress()
		{
			if (string.IsNullOrEmpty(BindToAddress))
				return null;
			return IPAddress.Parse(BindToAddress);
		}
	}

	public class SslCertificateConfig
	{
		/// <summary>
		/// Certificate source: "File" or "Store" (Store only works on Windows)
		/// </summary>
		public string Source { get; set; } = "File";

		// File-based certificate options (cross-platform)
		public string FilePath { get; set; }
		public string Password { get; set; }

		// Windows Certificate Store options (Windows-only, for backwards compatibility)
		public string StoreName { get; set; }
		public string StoreLocation { get; set; }
		public string Thumbprint { get; set; }
	}
}
