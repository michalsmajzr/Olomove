
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Olomove.Models.Users;

namespace Olomove.Controllers.Profiles;

[ApiController]
[Authorize]
[Route("api/user/profile")]
public class UserProfileController(
    UserManager<User> userManager
) : ControllerBase
{
    // GET: api/user/profile
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        return Ok(ToResponse(user));
    }

    // Údaje, které může uživatel upravovat.
    public class UpdateProfileRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = "";

        [Phone]
        [MaxLength(30)]
        public string? PhoneNumber { get; set; }
    }

    // PUT: api/user/profile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var user = await userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        var firstName = request.FirstName?.Trim();
        var lastName = request.LastName?.Trim();
        var phoneNumber = request.PhoneNumber?.Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            return BadRequest(
                "Jméno a příjmení jsou povinné."
            );
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber =
            string.IsNullOrWhiteSpace(phoneNumber)
                ? null
                : phoneNumber;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Message = "Profil se nepodařilo uložit.",
                Errors = result.Errors.Select(e => e.Description)
            });
        }

        return Ok(ToResponse(user));
    }

    private static object ToResponse(User user)
    {
        return new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.CreatedAt
        };
    }
}
