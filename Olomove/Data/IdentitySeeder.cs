using Microsoft.AspNetCore.Identity;
using Olomove.Models;

namespace Olomove.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Admin", "Lecturer", "Teacher", "User"];

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Role '{role}' se nepodařilo vytvořit.");
                }
            }
        }

        var email = configuration["InitialAdmin:Email"];
        var password = configuration["InitialAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                FirstName = configuration["InitialAdmin:FirstName"] ?? "Admin",
                LastName = configuration["InitialAdmin:LastName"] ?? "Olomove"
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Výchozího admina se nepodařilo vytvořit: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Roli Admin se nepodařilo přiřadit výchozímu uživateli.");
            }
        }
    }
}
