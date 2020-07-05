using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TandemBooking.Models;

namespace TandemBooking.Services
{
    public static class Policy
    {
        public const string IsValidated = "IsValidated";
        public const string IsAdmin = "IsAdmin";
        public const string IsPilot = "IsPilot";
    }

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTandemBookingAuthentication(this IServiceCollection services)
        {
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

            return services;
        }

        public static IServiceCollection AddTandemBookingAuthorization(this IServiceCollection services)
        {
            services
                .AddAuthorization(options =>
                {
                    options.AddPolicy(Policy.IsValidated, policy => policy.RequireAssertion(ctx => ctx.User.IsAdmin() || ctx.User.IsPilot()));
                    options.AddPolicy(Policy.IsAdmin, policy => policy.RequireClaim(ClaimsPrincipalExtensions.AdminClaim));
                    options.AddPolicy(Policy.IsPilot, policy => policy.RequireClaim(ClaimsPrincipalExtensions.PilotClaim));
                });

            return services;
        }

        /// <summary>
        ///     Add services that does not directly call third party services.
        ///     Requires implementations of INexmoService and IMailService to be available.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBookingServices(this IServiceCollection services)
        {
            services.AddTransient<BookingServiceDb>();
            services.AddTransient<BookingService>();

            services.AddTransient<MessageService>();
            services.AddTransient<SmsService>();

            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            return services;
        }
    }
}