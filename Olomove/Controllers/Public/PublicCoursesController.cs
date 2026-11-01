using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Courses;
using Olomove.Models.Courses.Enums;

namespace Olomove.Controllers.Public;

[ApiController]
[Route("api/courses")]
public class PublicCoursesController(
    ApplicationDbContext context
) : ControllerBase
{
    // GET: api/courses
    // Vrátí nabídku aktuálních a budoucích kurzů.
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var courses = await context.Courses
            .AsNoTracking()
            .AsSplitQuery()
            .Where(c => c.EndDate >= today)
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Room)
            .Include(c => c.CourseInstructors)
                .ThenInclude(ci => ci.Instructor)
            .Include(c => c.CourseDanceStyles)
                .ThenInclude(cd => cd.DanceStyle)
            .Include(c => c.CourseEnrollments)
            .Include(c => c.Students)
            .OrderBy(c => c.StartDate)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var result = courses.Select(course =>
        {
            var occupiedPlaces = GetOccupiedPlaces(course);

            return new
            {
                course.Id,
                course.Name,
                course.Description,
                course.CourseType,
                course.Level,
                course.StartDate,
                course.EndDate,
                course.Capacity,
                course.Price,

                RegisteredCount = course.CourseEnrollments.Count(
                    e => e.Status == CourseEnrollmentStatus.Registered
                ),

                OccupiedPlaces = occupiedPlaces,

                AvailablePlaces = Math.Max(
                    0,
                    course.Capacity - occupiedPlaces
                ),

                DanceStyles = course.CourseDanceStyles
                    .Where(cd => cd.DanceStyle != null)
                    .Select(cd => cd.DanceStyle!.Name)
                    .ToList(),

                Instructors = course.CourseInstructors
                    .Where(ci => ci.Instructor != null)
                    .Select(ci =>
                        ci.Instructor!.FirstName + " " +
                        ci.Instructor.LastName)
                    .ToList(),

                Sessions = course.Sessions
                    .OrderBy(s => s.Weekday)
                    .ThenBy(s => s.StartsAt)
                    .Select(s => new
                    {
                        s.Weekday,
                        s.StartsAt,
                        s.EndsAt,
                        Room = s.Room?.Name
                    })
                    .ToList()
            };
        }).ToList();

        return Ok(result);
    }

    // GET: api/courses/{id}
    // Vrátí detail jednoho konkrétního kurzu.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCourse(Guid id)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var course = await context.Courses
            .AsNoTracking()
            .AsSplitQuery()
            .Where(c => c.Id == id && c.EndDate >= today)
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Room)
            .Include(c => c.CourseInstructors)
                .ThenInclude(ci => ci.Instructor)
            .Include(c => c.CourseDanceStyles)
                .ThenInclude(cd => cd.DanceStyle)
            .Include(c => c.CourseEnrollments)
            .Include(c => c.Students)
            .FirstOrDefaultAsync();

        if (course == null)
        {
            return NotFound("Kurz nebyl nalezen.");
        }

        var occupiedPlaces = GetOccupiedPlaces(course);

        var result = new
        {
            course.Id,
            course.Name,
            course.Description,
            course.CourseType,
            course.Level,
            course.StartDate,
            course.EndDate,
            course.Capacity,
            course.Price,

            // Nastavení tanečních rolí
            course.BalancedRoles,
            course.MaxRoleDifference,
            course.AllowLeaderRegistration,
            course.AllowFollowerRegistration,

            // Aktuální obsazenost
            OccupiedPlaces = occupiedPlaces,

            AvailablePlaces = Math.Max(
                0,
                course.Capacity - occupiedPlaces
            ),

            // Taneční styly
            DanceStyles = course.CourseDanceStyles
                .Where(cd => cd.DanceStyle != null)
                .Select(cd => cd.DanceStyle!.Name)
                .ToList(),

            // Lektoři
            Instructors = course.CourseInstructors
                .Where(ci => ci.Instructor != null)
                .Select(ci =>
                    ci.Instructor!.FirstName + " " +
                    ci.Instructor.LastName)
                .ToList(),

            // Pravidelný rozvrh
            Sessions = course.Sessions
                .OrderBy(s => s.Weekday)
                .ThenBy(s => s.StartsAt)
                .Select(s => new
                {
                    s.Weekday,
                    s.StartsAt,
                    s.EndsAt,
                    Room = s.Room?.Name
                })
                .ToList()
        };

        return Ok(result);
    }

    // Pomocná metoda pro výpočet obsazenosti.
    private static int GetOccupiedPlaces(Course course)
    {
        var activeEnrollments = course.CourseEnrollments
            .Where(e => e.Status == CourseEnrollmentStatus.Registered)
            .ToList();

        // Registrovaní uživatelé
        var registeredUsers = activeEnrollments.Count;

        // Přihlášení partneři v rámci těchto přihlášek
        var registeredPartners = activeEnrollments.Count(e => e.SavedPartnerId.HasValue);

        // Studenti ze škol zůstávají samostatně evidovaní
        var studentsWithoutAccount = course.Students.Count;

        return registeredUsers + registeredPartners + studentsWithoutAccount;
    }
}