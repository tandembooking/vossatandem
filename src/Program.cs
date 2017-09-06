using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TandemBooking
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var hostingEnvironment = hostingContext.HostingEnvironment;
                    config
                        .AddJsonFile("appsettings.json", true, true)
                        .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("appsettings.local.json", true, true);

                    if (hostingEnvironment.IsDevelopment())
                    {
                        var assembly = Assembly.Load(new AssemblyName(hostingEnvironment.ApplicationName));
                        if (assembly != null)
                        {
                            config.AddUserSecrets(assembly, true);
                        }
                    }

                    config.AddEnvironmentVariables();

                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .WriteTo.LiterateConsole()
                        .WriteTo.RollingFile("log/tandembooking-{Date}.log")
                        .CreateLogger();

                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddSerilog();
                })
                .UseIISIntegration()
                .UseDefaultServiceProvider(
                    (context, options) =>
                    {
                        options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                    })
                .UseStartup<Startup>()
                .Build();
        }
    }
}