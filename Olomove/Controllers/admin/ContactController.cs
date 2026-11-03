using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.Admin
{
	[ApiController]
	[Route("api")]
	public class ContactController : ControllerBase
	{
		private readonly ApplicationDbContext _context;

		public ContactController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET /api/contact
		[HttpGet("contact")]
		public async Task<ActionResult<ContactPage>> GetContact()
		{
			var contact = await _context.ContactPages.FirstOrDefaultAsync();

			if (contact == null)
			{
				contact = new ContactPage();
				_context.ContactPages.Add(contact);
				await _context.SaveChangesAsync();
			}

			return Ok(contact);
		}

		// PUT /api/admin/contact
		[HttpPut("admin/contact")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> UpdateContact([FromBody] ContactPage dto)
		{
			var contact = await _context.ContactPages.FirstOrDefaultAsync();

			if (contact == null)
			{
				contact = new ContactPage();
				_context.ContactPages.Add(contact);
			}

			contact.Name = dto.Name;
			contact.Email = dto.Email;
			contact.Phone = dto.Phone;
			contact.City = dto.City;
			contact.Address = dto.Address;
			contact.PostCode = dto.PostCode;
			contact.MapUrl = dto.MapUrl;
			contact.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			return NoContent();
		}
	}
}