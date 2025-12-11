using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TDSProxy.Configuration;

namespace TDSProxy
{
	public sealed class TDSProxyService : IHostedService, IDisposable
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		public static bool VerboseLogging { get; private set; }
		public static bool VerboseLoggingInWrapper { get; private set; }
		public static bool SkipLoginProcessing { get; private set; }
		public static bool AllowUnencryptedConnections { get; private set; }

		private readonly HashSet<TDSListener> _listeners = new HashSet<TDSListener>();
		private readonly TdsProxySection _configuration;

		private bool _stopRequested;

		public TDSProxyService(IOptions<TdsProxySection> configuration)
		{
			_configuration = configuration.Value;
		}

		public TdsProxySection Configuration => _configuration;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Start(Environment.GetCommandLineArgs().Skip(1).ToArray());
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			Stop();
			return Task.CompletedTask;
		}

		public void Start(string[] args)
		{
			log.InfoFormat(
				"\r\n-----------------\r\nService Starting on {0} with security protocol {1}.\r\n-----------------\r\n",
				System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
				ServicePointManager.SecurityProtocol);

			if (args.Any(a => string.Equals(a, "debug", StringComparison.OrdinalIgnoreCase)))
			{
				log.Info("Calling Debugger.Break()");
				System.Diagnostics.Debugger.Break();
			}

			VerboseLogging = args.Any(a => string.Equals(a, "verbose", StringComparison.OrdinalIgnoreCase));
			if (VerboseLogging)
				log.Debug("Verbose logging is on.");

			VerboseLoggingInWrapper = args.Any(a => string.Equals(a, "wrapperverbose", StringComparison.OrdinalIgnoreCase));
			if (VerboseLoggingInWrapper)
				log.Debug("Verbose logging is on in TDS/SSL wrapper.");

			TDSProtocol.TDSPacket.DumpPackets = args.Any(a => string.Equals(a, "packetdump", StringComparison.OrdinalIgnoreCase));
			if (TDSProtocol.TDSPacket.DumpPackets)
				log.Debug("Packet dumping is on.");

			SkipLoginProcessing = args.Any(a => string.Equals(a, "skiplogin", StringComparison.OrdinalIgnoreCase));
			if (SkipLoginProcessing)
				log.Debug("Skipping login processing.");

			AllowUnencryptedConnections = args.Any(a => string.Equals(a, "allowunencrypted", StringComparison.OrdinalIgnoreCase));
			if (AllowUnencryptedConnections)
				log.Debug("Allowing unencrypted connections (but encryption must be supported because we will not allow unencrypted login).");

			_stopRequested = false;

			StartListeners();

			log.Info("TDSProxyService initialization complete.");
		}

		public void Stop()
		{
			log.Info("Stopping TDSProxyService");
			LogStats();
			_stopRequested = true;
			StopListeners();
			OnStopping(EventArgs.Empty);
			log.Info("\r\n----------------\r\nService stopped.\r\n----------------\r\n");
		}

		public bool StopRequested => _stopRequested;

		public event EventHandler Stopping;

		private void OnStopping(EventArgs e) => Stopping?.Invoke(this, e);

		private void StartListeners()
		{
			foreach (var listenerConfig in _configuration.Listeners)
				new TDSListener(this, listenerConfig);
		}

		private void StopListeners()
		{
			List<TDSListener> listeners;
			lock (_listeners)
				listeners = new List<TDSListener>(_listeners);

			foreach (var listener in listeners)
				listener.Dispose();
		}

		private void LogStats()
		{
			log.InfoFormat(
				"{0} active connections ({1} connections started since last restart, {2} connections collected without being closed first)",
				TDSConnection.ActiveConnectionCount,
				TDSConnection.TotalConnections,
				TDSConnection.UnclosedCollections);
		}

		internal void AddListener(TDSListener listener)
		{
			lock(_listeners)
				_listeners.Add(listener);
		}

		internal void RemoveListener(TDSListener listener)
		{
			lock (_listeners)
				_listeners.Remove(listener);
		}

		public void Dispose()
		{
			Stop();
		}
	}
}
