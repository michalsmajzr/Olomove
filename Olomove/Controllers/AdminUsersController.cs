using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Models;

namespace Olomove.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public class AdminUsersController(UserManager<User> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userManager.Users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Credit
            })
            .ToListAsync();

        var response = new List<object>();

        foreach (var user in users)
        {
            var identityUser = await userManager.FindByIdAsync(user.Id.ToString());
            IList<string> roles = identityUser is null
                ? []
                : await userManager.GetRolesAsync(identityUser);

            response.Add(new
            {
                user.Id,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                user.Email,
                Role = roles.FirstOrDefault() ?? "User",
                user.Credit
            });
        }

        return Ok(response);
    }
}
