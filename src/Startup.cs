using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TandemBooking.Models;
using TandemBooking.Services;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Serilog;

namespace TandemBooking
{
    public class Startup
    {
        private DateTime _startTime;

        public Startup(IHostingEnvironment env, IHostingEnvironment appEnv)
        {
            _startTime = DateTime.UtcNow;
            Console.WriteLine("Startup");

            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(appEnv.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile($"appsettings.local.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.LiterateConsole()
                .WriteTo.RollingFile("log/tandembooking-{Date}.log")
                .CreateLogger();

            Console.WriteLine($"Configuration Loaded {(DateTime.UtcNow - _startTime).TotalSeconds}");
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine($"Configure Services Start {(DateTime.UtcNow - _startTime).TotalSeconds}");

            // Add framework services.
            services.AddEntityFrameworkSqlServer()
                //.AddSqlite()
                .AddDbContext<TandemBookingContext>(options =>
                {
                    options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]);
                });

            services.AddDataProtection();

            services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<TandemBookingContext>()
                .AddUserManager<UserManager>()
                .AddDefaultTokenProviders();

            services
                .AddAuthorization(options =>
                {
                    options.AddPolicy("IsAdmin", policy => policy.RequireClaim(ClaimsPrincipalExtensions.AdminClaim));
                    options.AddPolicy("IsPilot", policy => policy.RequireClaim(ClaimsPrincipalExtensions.PilotClaim));
                });

            services.AddMvc();

            // Add application services.
            services.AddTransient(provider => new NexmoService(Configuration["Nexmo:Enable"] == "True", Configuration["Nexmo:ApiKey"], Configuration["Nexmo:ApiSecret"]));
            services.AddTransient(
                provider =>
                    new BookingCoordinatorSettings()
                    {
                        Name = Configuration["BookingCoordinator:Name"],
                        PhoneNumber = Configuration["BookingCoordinator:PhoneNumber"]
                    });
            services.AddTransient(provider => new MailSettings()
            {
                Enable = Configuration["Mail:Enable"] == "True",
                SmtpUser = Configuration["Mail:SmtpUser"],
                SmtpPassword = Configuration["Mail:SmtpPassword"],
                SmtpServer = Configuration["Mail:SmtpServer"],
                SmtpPort = int.Parse(Configuration["Mail:SmtpPort"]),
                FromName = Configuration["Mail:FromName"],
                FromAddress = Configuration["Mail:FromAddress"],
            });
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
            services.AddTransient<SmsService>();
            services.AddTransient<MessageService>();
            services.AddTransient<MailService>();

            services.AddTransient<BookingServiceDb>();
            services.AddTransient<BookingService>();

            Console.WriteLine($"Configure Services Done {(DateTime.UtcNow - _startTime).TotalSeconds}");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            Console.WriteLine($"Configure App Start {(DateTime.UtcNow - _startTime).TotalSeconds}");

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            loggerFactory.AddSerilog();
            appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // For more details on creating database during deployment see http://go.microsoft.com/fwlink/?LinkID=615859
                try
                {
                    using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>()
                        .CreateScope())
                    {
                        serviceScope.ServiceProvider.GetService<TandemBookingContext>()
                            .Database.Migrate();
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Unable to migrate database");
                }
            }

            //force domain name
            if (!string.IsNullOrEmpty(Configuration["Server:Host"]))
            {
                var host = Configuration["Server:Host"];
                app.Use(async (context, next) =>
                {
                    if (context.Request.Host.Value == host)
                    {
                        await next();
                    }
                    else
                    {
                        var url = context.Request.Scheme + "://" + host + context.Request.Path + context.Request.QueryString;
                        context.Response.Redirect(url);
                    }
                });
            }

            app.UseStaticFiles();
            app.UseIdentity();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            Console.WriteLine($"Configure App Done {(DateTime.UtcNow - _startTime).TotalSeconds}");
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        } 
    }
}
