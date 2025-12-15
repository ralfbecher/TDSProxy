using System.Collections.Generic;

namespace TDSProxy.Configuration
{
	public class TdsProxySection
	{
		public List<ListenerConfig> Listeners { get; set; } = new List<ListenerConfig>();
		public List<ServerConfig> Servers { get; set; } = new List<ServerConfig>();
	}

	public class ListenerConfig
	{
		public string Host { get; set; } = "0.0.0.0";
		public ushort Port { get; set; }

		/// <summary>
		/// TLS settings for client connections to this listener
		/// </summary>
		public ListenerTlsConfig Tls { get; set; }
	}

	public class ListenerTlsConfig
	{
		/// <summary>
		/// Enable TLS for incoming client connections
		/// </summary>
		public bool Enabled { get; set; } = false;

		/// <summary>
		/// Path to PFX/PKCS12 certificate file
		/// </summary>
		public string CertificatePath { get; set; }

		/// <summary>
		/// Password for the certificate file (if required)
		/// </summary>
		public string CertificatePassword { get; set; }

		/// <summary>
		/// Allowed TLS protocols (e.g., "Tls12", "Tls13", "Tls12,Tls13")
		/// Default: Tls12,Tls13
		/// </summary>
		public string Protocols { get; set; } = "Tls12,Tls13";
	}

	public class ServerConfig
	{
		public string Host { get; set; }
		public ushort Port { get; set; } = 1433;

		/// <summary>
		/// TLS settings for connections to this SQL Server
		/// </summary>
		public ServerTlsConfig Tls { get; set; }
	}

	public class ServerTlsConfig
	{
		/// <summary>
		/// Enable TLS for connection to SQL Server
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// Validate the SQL Server's certificate
		/// </summary>
		public bool ValidateCertificate { get; set; } = true;

		/// <summary>
		/// Trust the server certificate even if validation fails
		/// </summary>
		public bool TrustServerCertificate { get; set; } = false;

		/// <summary>
		/// Allowed TLS protocols (e.g., "Tls", "Tls11", "Tls12", "Tls13")
		/// Default: Tls,Tls11,Tls12,Tls13 (for compatibility)
		/// </summary>
		public string Protocols { get; set; } = "Tls,Tls11,Tls12,Tls13";
	}
}
