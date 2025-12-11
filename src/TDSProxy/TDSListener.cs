using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using TDSProxy.Authentication;
using TDSProxy.Configuration;

namespace TDSProxy
{
	class TDSListener : IDisposable
	{
		#region Log4Net
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		#endregion

		readonly TDSProxyService _service;
		readonly TcpListener _tcpListener;
		readonly CompositionContainer _mefContainer;
		volatile bool _stopped;

		internal readonly X509Certificate Certificate;

		public TDSListener(TDSProxyService service, ListenerElement configuration)
		{
			var insideAddresses = Dns.GetHostAddresses(configuration.ForwardToHost);
			if (0 == insideAddresses.Length)
			{
				log.ErrorFormat("Unable to resolve forwardToHost=\"{0}\" for listener {1}", configuration.ForwardToHost, configuration.Name);
				_stopped = true;
				return;
			}
			ForwardTo = new IPEndPoint(insideAddresses.First(), configuration.ForwardToPort);

			_service = service;

			var bindToEP = new IPEndPoint(configuration.GetBindToAddress() ?? IPAddress.Any, configuration.ListenOnPort);

			try
			{
				var catalog = new AggregateCatalog(from a in configuration.Authenticators
				                                   select new AssemblyCatalog(a.Dll));
				_mefContainer = new CompositionContainer(catalog);

				_authenticators = _mefContainer.GetExports<IAuthenticator>().ToArray();
				if (!_authenticators.Any())
				{
					throw new InvalidOperationException("No authenticators");
				}
			}
			catch (CompositionException ce)
			{
				log.Error(
					"Failed to find an authenticator. Composition errors:\r\n\t" +
					string.Join("\r\n\t", ce.Errors.Select(err => "Element: " + err.Element.DisplayName + ", Error: " + err.Description)),
					ce);
				Dispose();
				return;
			}
			catch (Exception e)
			{
				log.Error("Failed to find an authenticator", e);
				Dispose();
				return;
			}

			try
			{
				Certificate = LoadCertificate(configuration);
				if (Certificate == null)
				{
					log.Error("Failed to load SSL certificate - certificate is null");
					Dispose();
					return;
				}
			}
			catch (Exception e)
			{
				log.Error("Failed to load SSL certificate", e);
				Dispose();
				return;
			}

			_tcpListener = new TcpListener(bindToEP);
			_tcpListener.Start();
			_tcpListener.BeginAcceptTcpClient(AcceptConnection, _tcpListener);

			_service.AddListener(this);

			log.InfoFormat(
				"Listening on {0} and forwarding to {1} (SSL cert DN {2}; expires {5} serial {3}; authenticators {4})",
				bindToEP,
				ForwardTo,
				Certificate.Subject,
				Certificate.GetSerialNumberString(),
				string.Join(", ", from a in Authenticators select a.GetType().FullName),
				Certificate.GetExpirationDateString());
		}

		private X509Certificate2 LoadCertificate(ListenerElement configuration)
		{
			var certConfig = configuration.SslCertificate;
			if (certConfig == null)
			{
				throw new InvalidOperationException($"No SSL certificate configuration for listener {configuration.Name}");
			}

			if (string.Equals(certConfig.Source, "File", StringComparison.OrdinalIgnoreCase))
			{
				return LoadCertificateFromFile(certConfig, configuration.Name);
			}
			else if (string.Equals(certConfig.Source, "Store", StringComparison.OrdinalIgnoreCase))
			{
				return LoadCertificateFromStore(certConfig, configuration.Name);
			}
			else
			{
				throw new InvalidOperationException($"Unknown certificate source '{certConfig.Source}' for listener {configuration.Name}. Use 'File' or 'Store'.");
			}
		}

		private X509Certificate2 LoadCertificateFromFile(SslCertificateConfig certConfig, string listenerName)
		{
			if (string.IsNullOrEmpty(certConfig.FilePath))
			{
				throw new InvalidOperationException($"Certificate file path not specified for listener {listenerName}");
			}

			var filePath = certConfig.FilePath;
			if (!Path.IsPathRooted(filePath))
			{
				filePath = Path.Combine(AppContext.BaseDirectory, filePath);
			}

			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException($"Certificate file not found: {filePath}", filePath);
			}

			log.DebugFormat("Loading SSL certificate from file: {0}", filePath);

			if (string.IsNullOrEmpty(certConfig.Password))
			{
				return new X509Certificate2(filePath);
			}
			else
			{
				return new X509Certificate2(filePath, certConfig.Password, X509KeyStorageFlags.PersistKeySet);
			}
		}

		private X509Certificate2 LoadCertificateFromStore(SslCertificateConfig certConfig, string listenerName)
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				throw new PlatformNotSupportedException(
					$"Certificate store loading is only supported on Windows. " +
					$"Use 'File' source for cross-platform certificate loading on listener {listenerName}.");
			}

			if (string.IsNullOrEmpty(certConfig.Thumbprint))
			{
				throw new InvalidOperationException($"Certificate thumbprint not specified for listener {listenerName}");
			}

			var storeName = Enum.Parse<StoreName>(certConfig.StoreName ?? "My");
			var storeLocation = Enum.Parse<StoreLocation>(certConfig.StoreLocation ?? "CurrentUser");

			log.DebugFormat("Opening SSL certificate store {0}.{1}", storeLocation, storeName);

			using var store = new X509Store(storeName, storeLocation);
			store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
			var matching = store.Certificates.Find(X509FindType.FindByThumbprint, certConfig.Thumbprint, false);

			if (0 == matching.Count)
			{
				throw new InvalidOperationException(
					$"Failed to find SSL certificate with thumbprint '{certConfig.Thumbprint}' " +
					$"in location {storeLocation}, store {storeName}.");
			}

			return matching[0];
		}

		private Lazy<IAuthenticator>[] _authenticators;

		public IEnumerable<Lazy<IAuthenticator>> Authenticators =>
			!_stopped
				? (Lazy<IAuthenticator>[])_authenticators.Clone()
				: throw new ObjectDisposedException(nameof(TDSListener));

		public IPEndPoint ForwardTo { get; }

		private void AcceptConnection(IAsyncResult result)
		{
			try
			{
				// Get connection
				TcpClient readClient = ((TcpListener)result.AsyncState).EndAcceptTcpClient(result);

				//Log as Info so we have the open (and the close elsewhere)
				log.InfoFormat("Accepted connection from {0} on {1}, will forward to {2}", readClient.Client.RemoteEndPoint, readClient.Client.LocalEndPoint, ForwardTo);

				// Handle stop requested
				if (_service?.StopRequested == true)
				{
					log.Info("Service was ending, closing connection and returning.");
					readClient.Close();
					return;
				}

				// Process this connection
				new TDSConnection(_service, this, readClient, ForwardTo);
			}
			catch (ObjectDisposedException) { /* We're shutting down, ignore */ }
			catch (Exception e)
			{
				log.Fatal("Error in AcceptConnection.", e);
			}

			// Listen for next connection -- Do this here so we accept new connections even if this attempt to accept failed.
			if (!_stopped)
			{
				try
				{
					_tcpListener?.BeginAcceptTcpClient(AcceptConnection, _tcpListener);
				}
				catch (ObjectDisposedException) { /* We're shutting down, ignore */ }
			}
		}

		public void Dispose()
		{
			if (!_stopped)
			{
				_stopped = true;
				_service?.RemoveListener(this);
				_tcpListener?.Stop();
				if (null != _mefContainer)
				{
					if (null != _authenticators)
						_mefContainer.ReleaseExports(_authenticators);
					_mefContainer.Dispose();
				}
				_authenticators = null;
			}
		}
	}
}
