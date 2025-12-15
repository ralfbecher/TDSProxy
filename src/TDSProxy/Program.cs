using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TDSProxy.Configuration;

namespace TDSProxy
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static async Task Main(string[] args)
		{
			// Set current directory to application base directory
			Environment.CurrentDirectory = AppContext.BaseDirectory;

			// Configure log4net
			var logConfigPath = Path.Combine(AppContext.BaseDirectory, "log4net.config");
			if (File.Exists(logConfigPath))
			{
				log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(logConfigPath));
			}

			var host = Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((context, config) =>
				{
					config.SetBasePath(AppContext.BaseDirectory);
					config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
					config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
					config.AddEnvironmentVariables("TDSPROXY_");
					config.AddCommandLine(args);
				})
				.ConfigureServices((context, services) =>
				{
					// Bind configuration at root level to match user's config format
					services.Configure<TdsProxySection>(context.Configuration);

					// Register the proxy service as a hosted service
					services.AddHostedService<TDSProxyService>();
				})
				.UseConsoleLifetime()
				.Build();

			await host.RunAsync();
		}
	}
}
