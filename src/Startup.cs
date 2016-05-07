using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TandemBooking.Models;
using TandemBooking.Services;

namespace TandemBooking
{
    public class Startup
    {
        private DateTime _startTime;

        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            _startTime = DateTime.UtcNow;
            Console.WriteLine("Startup");

            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
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

            Console.WriteLine($"Configuration Loaded {(DateTime.UtcNow - _startTime).TotalSeconds}");
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine($"Configure Services Start {(DateTime.UtcNow - _startTime).TotalSeconds}");

            // Add framework services.
            services.AddEntityFramework()
                .AddSqlServer()
                //.AddSqlite()
                .AddDbContext<TandemBookingContext>(options =>
                {
                    options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]);
                });


            services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonLetterOrDigit = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 6;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<TandemBookingContext>()
                .AddUserManager<UserManager>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            // Add application services.
            services.AddTransient(provider => new NexmoService(Configuration["Nexmo:ApiKey"], Configuration["Nexmo:ApiSecret"]));
            services.AddTransient(
                provider =>
                    new BookingCoordinatorSettings()
                    {
                        Name = Configuration["BookingCoordinator:Name"],
                        PhoneNumber = Configuration["BookingCoordinator:PhoneNumber"]
                    });
            services.AddTransient(provider => new MailSettings()
            {
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

            services.AddTransient<BookingService>();

            Console.WriteLine($"Configure Services Done {(DateTime.UtcNow - _startTime).TotalSeconds}");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            Console.WriteLine($"Configure App Start {(DateTime.UtcNow - _startTime).TotalSeconds}");

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

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
                catch { }
            }

            app.UseIISPlatformHandler(options => options.AuthenticationDescriptions.Clear());
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
        public static void Main(string[] args) => Microsoft.AspNet.Hosting.WebApplication.Run<Startup>(args);
    }
}
