
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.lecturer;

[ApiController]
[Authorize(Roles = "Instructor,Teacher,Admin")]
[Route("api/lecturer/lessons")]
public class LecturerLessonsController(
    ApplicationDbContext context,
    UserManager<User> userManager
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLessons(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
    {
        if (from == default || to == default)
        {
            return BadRequest("Je nutné zadat období od a do.");
        }

        if (from > to)
        {
            return BadRequest("Datum od nesmí být větší než datum do.");
        }

        if (to.DayNumber - from.DayNumber > 366)
        {
            return BadRequest("Maximální rozsah je 366 dní.");
        }

        var userId = userManager.GetUserId(User);

        if (!Guid.TryParse(userId, out var lecturerId))
        {
            return Unauthorized();
        }

        var courses = await context.Courses
            .AsNoTracking()
            .Where(c =>
                c.CourseInstructors.Any(
                    ci => ci.InstructorId == lecturerId
                )
                && c.StartDate <= to
                && c.EndDate >= from
            )
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Room)
            .ToListAsync();

        var lessons = courses
            .SelectMany(course =>
                course.Sessions.SelectMany(session =>
                    Enumerable.Range(
                        0,
                        to.DayNumber - from.DayNumber + 1
                    )
                    .Select(offset => from.AddDays(offset))
                    .Where(date =>
                        date >= course.StartDate &&
                        date <= course.EndDate &&
                        (int)session.Weekday ==
                            ((int)date.DayOfWeek + 6) % 7
                    )
                    .Select(date => new
                    {
                        Id = $"{session.Id}-{date:yyyyMMdd}",
                        SessionId = session.Id,
                        CourseId = course.Id,
                        CourseName = course.Name,
                        Date = date,
                        StartsAt = session.StartsAt,
                        EndsAt = session.EndsAt,
                        Room = session.Room?.Name
                    })
                )
            )
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartsAt)
            .ThenBy(x => x.CourseName)
            .ToList();

        return Ok(lessons);
    }
}
