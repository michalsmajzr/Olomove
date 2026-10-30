using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Courses;

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

        // Partneři přihlášení společně s uživatelem také zabírají místo.
        // Počítáme pouze partnery aktivních (Registered) přihlášek.
        var courseIds = courses.Select(c => c.Id).ToList();
        var partnerCounts = await context.CourseEnrollmentPartners
            .AsNoTracking()
            .Where(p => courseIds.Contains(p.Enrollment.CourseId) &&
                        p.Enrollment.Status == CourseEnrollmentStatus.Registered)
            .GroupBy(p => p.Enrollment.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        var result = courses.Select(course => new
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

            OccupiedPlaces = GetOccupiedPlaces(
                course,
                partnerCounts.GetValueOrDefault(course.Id)
            ),

            AvailablePlaces = Math.Max(
                0,
                course.Capacity - GetOccupiedPlaces(
                    course,
                    partnerCounts.GetValueOrDefault(course.Id)
                )
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

        var partnerCount = await context.CourseEnrollmentPartners
            .AsNoTracking()
            .CountAsync(p =>
                p.Enrollment.CourseId == id &&
                p.Enrollment.Status == CourseEnrollmentStatus.Registered);

        var occupiedPlaces = GetOccupiedPlaces(course, partnerCount);

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
    private static int GetOccupiedPlaces(Course course, int registeredPartners)
    {
        var registeredUsers = course.CourseEnrollments
            .Count(e =>
                e.Status == CourseEnrollmentStatus.Registered
            );

        // Studenti ze škol zůstávají samostatně evidovaní.
        var studentsWithoutAccount = course.Students.Count;

        return registeredUsers + registeredPartners + studentsWithoutAccount;
    }
}
