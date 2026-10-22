using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;

using WebApi.Application.Common;
using WebApi.Infrastructure.Identity.Configuration;

namespace WebApi.Infrastructure.Identity
{
    internal sealed class IdentitySeeder(RoleManager<IdentityRole<Guid>> roleManager, UserManager<IdentityUser<Guid>> userManager, IOptions<BootstrapAdminOptions> bootstrapOptions)
    {
        public async Task SeedRolesAsync()
        {
            string[] roles =
            [
                SecurityRoles.Admin,
                SecurityRoles.SuperAdmin
            ];

            foreach (var roleName in roles)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                    continue;

                var result = await roleManager.CreateAsync(
                    new IdentityRole<Guid>
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName
                    });

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(error => error.Code));
                    throw new InvalidOperationException($"Impossible de créer le rôle {roleName} : {errors}");
                }
            }
        }

        public async Task SeedSuperAdminAsync()
        {
            var options = bootstrapOptions.Value;

            if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
                throw new InvalidOperationException("La configuration BootstrapAdmin est incomplète.");

            await SeedRolesAsync();

            var account = await userManager.FindByEmailAsync(options.Email);
            if (account is null)
            {
                account = new IdentityUser<Guid>
                {
                    Id = Guid.NewGuid(),
                    UserName = options.Email,
                    Email = options.Email
                };

                var createResult = await userManager.CreateAsync(
                    account,
                    options.Password);

                EnsureSuccess(createResult, "créer le superadministrateur");
            }

            if (!await userManager.IsInRoleAsync(account, SecurityRoles.SuperAdmin))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    account,
                    SecurityRoles.SuperAdmin);

                EnsureSuccess(roleResult, "affecter le rôle SuperAdmin");
            }
        }

        private static void EnsureSuccess(IdentityResult result, string operation)
        {
            if (result.Succeeded)
                return;

            var errors = string.Join("; ", result.Errors.Select(error => error.Code));

            throw new InvalidOperationException($"Impossible de {operation} : {errors}");
        }
    }
}
