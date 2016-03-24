using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using TandemBooking.Models;

namespace TandemBooking.Services
{
    public class UserManager : UserManager<ApplicationUser>
    {
        public UserManager(
            IUserStore<ApplicationUser> store,
            IOptions<IdentityOptions> optionsAccessor,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IEnumerable<IUserValidator<ApplicationUser>> userValidators,
            IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            IServiceProvider services,
            ILogger<UserManager<ApplicationUser>> logger,
            IHttpContextAccessor contextAccessor
            )
            : base(
                store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors,
                services, logger, contextAccessor)
        {

        }

        public override async Task<IList<Claim>> GetClaimsAsync(ApplicationUser user)
        {
            //Get normal claims
            var claims = await base.GetClaimsAsync(user);

            //Add custom claims
            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimsPrincipalExtensions.AdminClaim, "true"));
            }
            if (user.IsPilot)
            {
                claims.Add(new Claim(ClaimsPrincipalExtensions.PilotClaim, "true"));
            }

            return claims;
        }
    }
}
