using Microsoft.AspNetCore.Identity;
using Olomove.Constants;
using Olomove.Models;

namespace Olomove.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Seed roles
        await SeedRolesAsync(roleManager);

        // Seed initial admin user
        await SeedInitialAdminAsync(userManager, configuration);
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        foreach (var roleName in UserRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new Role(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Role '{roleName}' se nepodařilo vytvořit: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    private static async Task SeedInitialAdminAsync(UserManager<User> userManager, IConfiguration configuration)
    {
        var email = configuration["InitialAdmin:Email"];
        var password = configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(email);
        if (adminUser is null)
        {
            adminUser = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = configuration["InitialAdmin:FirstName"] ?? "Admin",
                LastName = configuration["InitialAdmin:LastName"] ?? "Olomove"
            };

            var result = await userManager.CreateAsync(adminUser, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Výchozího admina se nepodařilo vytvořit: {errors}");
            }
        }

        // Assign Admin role
        if (!await userManager.IsInRoleAsync(adminUser, UserRoles.Admin))
        {
            var result = await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Roli Admin se nepodařilo přiřadit: {errors}");
            }
        }
    }
}