
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Users;

namespace Olomove.Controllers.Profile;

[ApiController]
[Authorize]
[Route("api/user/my-courses")]
public class MyCoursesController(
    ApplicationDbContext context,
    UserManager<User> userManager
) : ControllerBase
{
    // GET: api/user/my-courses
    [HttpGet]
    public async Task<IActionResult> GetMyCourses()
    {
        // Zjistíme přihlášeného uživatele.
        var user = await userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        // Načteme pouze přihlášky tohoto uživatele.
        var enrollments = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.UserId == user.Id)
            .OrderByDescending(e => e.Course.StartDate)
            .Select(e => new
            {
                e.Id,
                e.Status,
                e.DanceRole,
                e.Rotation,
                e.CreatedAt,

                // Kolik kreditů stálo celé přihlášení.
                e.PaidCredits,

                // Partner je volitelný.
                Partner = context.CourseEnrollmentPartners
                    .Where(p => p.CourseEnrollmentId == e.Id)
                    .Select(p => new
                    {
                        p.SavedPartnerId,
                        p.FirstName,
                        p.LastName,
                        p.DanceRole,
                        p.Rotation
                    })
                    .FirstOrDefault(),

                // Informace o přihlášeném kurzu.
                Course = new
                {
                    e.Course.Id,
                    e.Course.Name,
                    e.Course.CourseType,
                    e.Course.Level,
                    e.Course.StartDate,
                    e.Course.EndDate,
                    e.Course.Description,

                    Sessions = e.Course.Sessions
                        .Select(s => new
                        {
                            s.Weekday,
                            s.StartsAt,
                            s.EndsAt,
                            Room = s.Room != null
                                ? s.Room.Name
                                : null
                        })
                        .ToList(),

                    Instructors = e.Course.CourseInstructors
                        .Where(i => i.Instructor != null)
                        .Select(i =>
                            i.Instructor!.FirstName + " " +
                            i.Instructor.LastName)
                        .ToList()
                }
            })
            .ToListAsync();

        return Ok(new
        {
            credit = user.Credit,
            enrollments
        });
    }
}
    