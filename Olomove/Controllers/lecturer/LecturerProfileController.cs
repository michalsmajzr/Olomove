
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Olomove.Models;

namespace Olomove.Controllers.lecturer;

[ApiController]
[Authorize(Roles = "Instructor,Teacher,Admin")]
[Route("api/lecturer/profile")]
public class LecturerProfileController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public LecturerProfileController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    // GET: api/lecturer/profile
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            user.Email,
            user.PhoneNumber,
            user.CreatedAt
        });
    }

    public class UpdateProfileRequest
    {
        [Required(ErrorMessage = "Jméno je povinné.")]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Příjmení je povinné.")]
        [MaxLength(100)]
        public string LastName { get; set; } = "";

        [Phone(ErrorMessage = "Telefonní číslo nemá platný formát.")]
        [MaxLength(30)]
        public string? PhoneNumber { get; set; }
    }

    // PUT: api/lecturer/profile
    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        var firstName = request.FirstName?.Trim();
        var lastName = request.LastName?.Trim();
        var phoneNumber = request.PhoneNumber?.Trim();

        if (string.IsNullOrWhiteSpace(firstName))
            return BadRequest("Jméno je povinné.");

        if (string.IsNullOrWhiteSpace(lastName))
            return BadRequest("Příjmení je povinné.");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                Message = "Profil se nepodařilo uložit.",
                Errors = result.Errors.Select(e => e.Description)
            });
        }

        return Ok(new
        {
            Message = "Profil byl úspěšně aktualizován.",
            user.Id,
            user.FirstName,
            user.LastName,
            FullName = $"{user.FirstName} {user.LastName}",
            user.Email,
            user.PhoneNumber,
            user.CreatedAt
        });
    }
}
