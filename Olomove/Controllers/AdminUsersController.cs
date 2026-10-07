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

    public class ChangeRoleRequest
    {
        public required string Role { get; set; }
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        if (request.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Roli Administrátor nelze takto přiřadit.");
        }

        var currentUserId = userManager.GetUserId(User);
        if (currentUserId == id.ToString())
        {
            return BadRequest("Nemůžete změnit roli sami sobě.");
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound("Uživatel nebyl nalezen.");
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains("Admin"))
        {
            return BadRequest("Nelze změnit roli jinému administrátorovi.");
        }

        if (currentRoles.Any())
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest("Nepodařilo se odebrat stávající roli uživatele.");
            }
        }

        var addResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!addResult.Succeeded)
        {
            if (currentRoles.Any())
            {
                await userManager.AddToRolesAsync(user, currentRoles);
            }
            return BadRequest($"Role '{request.Role}' neexistuje nebo se ji nepodařilo přiřadit.");
        }

        return NoContent();
    }
}
