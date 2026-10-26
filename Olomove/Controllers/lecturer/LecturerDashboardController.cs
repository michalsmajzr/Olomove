
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Users;

namespace Olomove.Controllers.Lecturer;

[ApiController]
[Authorize(Roles = "Instructor,Teacher,Admin")]
[Route("api/lecturer/dashboard")]
public class LecturerDashboardController(
    ApplicationDbContext context,
    UserManager<User> userManager
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = userManager.GetUserId(User);

        if (!Guid.TryParse(userId, out var lecturerId))
        {
            return Unauthorized();
        }

        var courses = await context.Courses
            .AsNoTracking()
            .Where(c => c.CourseInstructors.Any(
                ci => ci.InstructorId == lecturerId))
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Room)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var until = today.AddDays(30);

        var lessons = courses
            .SelectMany(course =>
                course.Sessions.SelectMany(session =>
                    Enumerable.Range(0, until.DayNumber - today.DayNumber + 1)
                        .Select(offset => today.AddDays(offset))
                        .Where(date =>
                            date >= course.StartDate &&
                            date <= course.EndDate &&
                            (int)session.Weekday ==
                                ((int)date.DayOfWeek + 6) % 7)
                        .Select(date => new
                        {
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
            .Take(5)
            .ToList();

        return Ok(new
        {
            ActiveCourses = courses.Count(c => c.EndDate >= today),
            UpcomingLessons = lessons
        });
    }
}
