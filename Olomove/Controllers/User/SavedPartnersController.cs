using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Users;

namespace Olomove.Controllers.Partners;

[ApiController]
[Authorize]
[Route("api/user/saved-partners")]
public class SavedPartnersController(
    ApplicationDbContext context,
    UserManager<User> userManager
) : ControllerBase
{
    public class SavePartnerRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = "";

        [EmailAddress]
        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(32)]
        public string? PhoneNumber { get; set; }
    }

    // GET /api/user/saved-partners
    [HttpGet]
    public async Task<IActionResult> GetSavedPartners()
    {
        if (!Guid.TryParse(userManager.GetUserId(User), out var userId))
            return Unauthorized();

        var partners = await context.SavedPartners
            .AsNoTracking()
            .Where(p => p.OwnerUserId == userId)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new
            {
                p.Id,
                p.FirstName,
                p.LastName,
                p.Email,
                p.PhoneNumber
            })
            .ToListAsync();

        return Ok(partners);
    }

    // POST /api/user/saved-partners
    [HttpPost]
    public async Task<IActionResult> AddSavedPartner(
        [FromBody] SavePartnerRequest request)
    {
        if (!Guid.TryParse(userManager.GetUserId(User), out var userId))
            return Unauthorized();

        var firstName = request.FirstName?.Trim();
        var lastName = request.LastName?.Trim();
        var email = request.Email?.Trim();
        var phoneNumber = request.PhoneNumber?.Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
            return BadRequest("Jméno a příjmení jsou povinné.");

        if (string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(phoneNumber))
            return BadRequest("Vyplňte e-mail nebo telefonní číslo.");

        var partner = new SavedPartner
        {
            FirstName = firstName,
            LastName = lastName,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber,
            OwnerUserId = userId
        };

        context.SavedPartners.Add(partner);
        await context.SaveChangesAsync();

        return Ok(ToResponse(partner));
    }

    // PUT /api/user/saved-partners/{id}
    // Smí měnit pouze vlastník uloženého partnera.
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateSavedPartner(
        Guid id, [FromBody] SavePartnerRequest request)
    {
        if (!Guid.TryParse(userManager.GetUserId(User), out var userId))
            return Unauthorized();

        var partner = await context.SavedPartners
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerUserId == userId);

        if (partner == null)
            return NotFound("Partner nebyl nalezen.");

        var firstName = request.FirstName?.Trim();
        var lastName = request.LastName?.Trim();
        var email = request.Email?.Trim();
        var phoneNumber = request.PhoneNumber?.Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
            return BadRequest("Jméno a příjmení jsou povinné.");

        if (string.IsNullOrWhiteSpace(email) &&
            string.IsNullOrWhiteSpace(phoneNumber))
            return BadRequest("Vyplňte e-mail nebo telefonní číslo.");

        partner.FirstName = firstName;
        partner.LastName = lastName;
        partner.Email = string.IsNullOrWhiteSpace(email) ? null : email;
        partner.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber;

        await context.SaveChangesAsync();
        return Ok(ToResponse(partner));
    }

    private static object ToResponse(SavedPartner partner) => new
    {
        partner.Id,
        partner.FirstName,
        partner.LastName,
        partner.Email,
        partner.PhoneNumber
    };
}
