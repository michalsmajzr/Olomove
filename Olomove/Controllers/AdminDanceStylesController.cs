using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/dancestyles")]
public class AdminDanceStylesController(ApplicationDbContext dbContext) : ControllerBase
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
            .Select(s => new { s.Id, s.Name, s.Description })
            .ToList();

        return Ok(styles);
    }
}