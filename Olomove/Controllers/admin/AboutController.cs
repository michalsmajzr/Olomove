using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.Admin;

[ApiController]
[Route("api")]
public class AboutController : ControllerBase
{
	private readonly ApplicationDbContext _context;

	public AboutController(ApplicationDbContext context)
	{
		_context = context;
	}

	// GET /api/about
	[HttpGet("about")]
	public async Task<ActionResult<AboutPage>> GetAbout()
	{
		var about = await _context.AboutPages.FirstOrDefaultAsync();

		if (about == null)
		{
			about = new AboutPage
			{
				Title = "Nadpis",
				Content = "Obsah",
				UpdatedAt = DateTime.UtcNow
			};
			_context.AboutPages.Add(about);
			await _context.SaveChangesAsync();
		}

		return Ok(about);
	}

	// PUT /api/admin/about
	[HttpPut("admin/about")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> UpdateAbout([FromBody] AboutPageUpdateDto dto)
	{
		var about = await _context.AboutPages.FirstOrDefaultAsync();

		if (about == null)
		{
			about = new AboutPage();
			_context.AboutPages.Add(about);
		}

		about.Title = dto.Title;
		about.Content = dto.Content;
		about.UpdatedAt = DateTime.UtcNow;

		await _context.SaveChangesAsync();

		return NoContent();
	}
}

public class AboutPageUpdateDto
{
	public string Title { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
}
