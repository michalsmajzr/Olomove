using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/rooms")]
public class RoomController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateRoom(Room room)
    {
        if (string.IsNullOrWhiteSpace(room.Name))
        {
            return BadRequest("Název sálu je povinný.");
        }

        if (room.Capacity < 1)
        {
            return BadRequest("Kapacita musí být alespoň 1.");
        }

        room.Id = Guid.NewGuid();
        room.Name = room.Name.Trim();
        room.Description = string.IsNullOrWhiteSpace(room.Description)
            ? null
            : room.Description.Trim();
        room.CreatedAt = DateTimeOffset.UtcNow;

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            room.Id,
            room.Name,
            room.Capacity,
            room.Description
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRooms()
    {
        var rooms = await dbContext.Rooms
            .Select(room => new { room.Id, room.Name, room.Capacity, room.Description })
            .ToListAsync();

        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var room = await dbContext.Rooms
            .Where(room => room.Id == id)
            .Select(room => new { room.Id, room.Name, room.Capacity, room.Description })
            .FirstOrDefaultAsync();

        return room is null
            ? NotFound("Sál nebyl nalezen.")
            : Ok(room);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateRoom(Guid id, Room request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Název sálu je povinný.");
        }

        if (request.Capacity < 1)
        {
            return BadRequest("Kapacita musí být alespoň 1.");
        }

        var room = await dbContext.Rooms.FindAsync(id);
        if (room is null)
        {
            return NotFound("Sál nebyl nalezen.");
        }

        room.Name = request.Name.Trim();
        room.Capacity = request.Capacity;
        room.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            room.Id,
            room.Name,
            room.Capacity,
            room.Description
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        var room = await dbContext.Rooms.FindAsync(id);
        if (room is null)
        {
            return NotFound("Sál nebyl nalezen.");
        }

        dbContext.Rooms.Remove(room);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}