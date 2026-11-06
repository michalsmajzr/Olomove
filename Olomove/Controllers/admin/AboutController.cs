using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.Admin;

[ApiController]
[Route("api")]
public class AboutController(ApplicationDbContext context) : ControllerBase
{

	// GET /api/about
	[HttpGet("about")]
	public async Task<ActionResult<AboutPage>> GetAbout()
	{
		var about = await context.AboutPages.FirstOrDefaultAsync();

		if (about == null)
		{
			about = new AboutPage
			{
				Title = "Nadpis",
				Content = "Obsah",
				UpdatedAt = DateTime.UtcNow
			};
            context.AboutPages.Add(about);
			await context.SaveChangesAsync();
		}

		return Ok(about);
	}

	// PUT /api/admin/about
	[HttpPut("admin/about")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> UpdateAbout([FromBody] AboutPageUpdateDto dto)
	{
		var about = await context.AboutPages.FirstOrDefaultAsync();

		if (about == null)
		{
			about = new AboutPage();
            context.AboutPages.Add(about);
		}

		about.Title = dto.Title;
		about.Content = dto.Content;
		about.UpdatedAt = DateTime.UtcNow;

		await context.SaveChangesAsync();

		return NoContent();
	}
}

public class AboutPageUpdateDto
{
	public string Title { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
}
