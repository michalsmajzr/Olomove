using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/dancestyles")]
public class DanceStylesController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateDanceStyle(DanceStyle danceStyle)
    {
        if (string.IsNullOrWhiteSpace(danceStyle.Name))
        {
            return BadRequest("Název stylu tance je povinný.");
        }

        danceStyle.Id = Guid.NewGuid();
        danceStyle.CreatedAt = DateTimeOffset.UtcNow;

        dbContext.DanceStyles.Add(danceStyle);
        await dbContext.SaveChangesAsync();

        return Ok(danceStyle);
    }

    [HttpGet]
    public IActionResult GetAllDanceStyles()
    {
        var styles = dbContext.DanceStyles
            .Select(s => new { s.Id, s.Name, s.Genre, s.Description })
            .ToList();

        return Ok(styles);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDanceStyle(Guid id)
    {
        var danceStyle = await dbContext.DanceStyles
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Name, s.Genre, s.Description })
            .FirstOrDefaultAsync();

        return danceStyle is null
            ? NotFound("Tanec nebyl nalezen.")
            : Ok(danceStyle);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDanceStyle(Guid id, DanceStyle request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Název stylu tance je povinný.");
        }

        var danceStyle = await dbContext.DanceStyles.FindAsync(id);
        if (danceStyle is null)
        {
            return NotFound("Tanec nebyl nalezen.");
        }

        danceStyle.Name = request.Name.Trim();
        danceStyle.Genre = string.IsNullOrWhiteSpace(request.Genre) ? null : request.Genre.Trim();
        danceStyle.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            danceStyle.Id,
            danceStyle.Name,
            danceStyle.Genre,
            danceStyle.Description
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDanceStyle(Guid id)
    {
        var danceStyle = await dbContext.DanceStyles.FindAsync(id);
        if (danceStyle is null)
        {
            return NotFound("Tanec nebyl nalezen.");
        }

        dbContext.DanceStyles.Remove(danceStyle);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}