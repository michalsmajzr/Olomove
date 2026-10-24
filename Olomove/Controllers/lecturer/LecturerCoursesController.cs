
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.lecturer;

[ApiController]
[Authorize(Roles = "Instructor,Teacher,Admin")]
[Route("api/lecturer/courses")]
public class LecturerCoursesController(
    ApplicationDbContext context,
    UserManager<User> userManager
) : ControllerBase
{
    // GET: api/lecturer/courses
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
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
            )
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Room)
            .OrderBy(c => c.StartDate)
            .ThenBy(c => c.Name)
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

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

            IsActive =
                course.StartDate <= today &&
                course.EndDate >= today,

            Sessions = course.Sessions
                .OrderBy(s => s.Weekday)
                .ThenBy(s => s.StartsAt)
                .Select(session => new
                {
                    session.Id,
                    session.Weekday,
                    session.StartsAt,
                    session.EndsAt,
                    Room = session.Room?.Name
                })
                .ToList()
        });

        return Ok(result);
    }

    
    // GET: api/lecturer/courses/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCourseDetail(Guid id)
    {
        var userId = userManager.GetUserId(User);

        if (!Guid.TryParse(userId, out var lecturerId))
        {
            return Unauthorized();
        }

        var course = await context.Courses
            .AsNoTracking()
            .Where(c =>
                c.Id == id &&
                c.CourseInstructors.Any(
                    ci => ci.InstructorId == lecturerId
                )
            )
            .Include(c => c.Sessions)
                .ThenInclude(s => s.Room)
            .FirstOrDefaultAsync();

        if (course == null)
        {
            return NotFound(
                "Kurz nebyl nalezen nebo k němu nemáte přístup."
            );
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

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

            IsActive =
                course.StartDate <= today &&
                course.EndDate >= today,

            Sessions = course.Sessions
                .OrderBy(s => s.Weekday)
                .ThenBy(s => s.StartsAt)
                .Select(session => new
                {
                    session.Id,
                    session.Weekday,
                    session.StartsAt,
                    session.EndsAt,
                    Room = session.Room?.Name
                })
                .ToList()
        };

        return Ok(result);
    }

    
    // GET: api/lecturer/courses/{id}/students
    [HttpGet("{id:guid}/students")]
    public async Task<IActionResult> GetCourseStudents(Guid id)
    {
        var userId = userManager.GetUserId(User);

        if (!Guid.TryParse(userId, out var lecturerId))
        {
            return Unauthorized();
        }

        // Lektor smí vidět pouze studenty kurzů,
        // ke kterým je přiřazený.
        var course = await context.Courses
            .AsNoTracking()
            .Where(c =>
                c.Id == id &&
                c.CourseInstructors.Any(
                    ci => ci.InstructorId == lecturerId
                )
            )
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Capacity
            })
            .FirstOrDefaultAsync();

        if (course == null)
        {
            return NotFound(
                "Kurz nebyl nalezen nebo k němu nemáte přístup."
            );
        }

        // Načtení přihlášek včetně údajů uživatelů.
        var enrollments = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e =>
                e.CourseId == id &&
                e.Status == CourseEnrollmentStatus.Registered
            )
            .Include(e => e.User)
            .OrderBy(e => e.User.LastName)
            .ThenBy(e => e.User.FirstName)
            .Select(e => new
            {
                EnrollmentId = e.Id,
                StudentId = e.UserId,
                FirstName = e.User.FirstName,
                LastName = e.User.LastName,
                FullName = e.User.FirstName + " " + e.User.LastName,
                Email = e.User.Email,
                PhoneNumber = e.User.PhoneNumber,
                DanceRole = e.DanceRole,
                Rotation = e.Rotation,
                RegisteredAt = e.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            CourseId = course.Id,
            CourseName = course.Name,
            Capacity = course.Capacity,
            RegisteredCount = enrollments.Count,
            AvailablePlaces = Math.Max(
                0,
                course.Capacity - enrollments.Count
            ),
            Students = enrollments
        });
    }

    
    // GET: api/lecturer/courses/{id}/students/{studentId}
    [HttpGet("{id:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> GetCourseStudentDetail(
        Guid id,
        Guid studentId)
    {
        var userId = userManager.GetUserId(User);

        if (!Guid.TryParse(userId, out var lecturerId))
            return Unauthorized();

        // Kontrola, zda lektor vyučuje požadovaný kurz.
        var hasAccess = await context.Courses
            .AsNoTracking()
            .AnyAsync(c =>
                c.Id == id &&
                c.CourseInstructors.Any(ci =>
                    ci.InstructorId == lecturerId
                )
            );

        if (!hasAccess)
        {
            return NotFound(
                "Kurz nebyl nalezen nebo k němu nemáte přístup."
            );
        }

        // Student musí mít aktivní přihlášku na daný kurz.
        var student = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e =>
                e.CourseId == id &&
                e.UserId == studentId &&
                e.Status == CourseEnrollmentStatus.Registered
            )
            .Select(e => new
            {
                e.UserId,
                e.User.FirstName,
                e.User.LastName,
                FullName = e.User.FirstName + " " + e.User.LastName,
                e.User.Email,
                e.User.PhoneNumber,
                e.DanceRole,
                e.Rotation,
                RegisteredAt = e.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (student == null)
            return NotFound("Student nebyl nalezen.");

        // Další kurzy studenta, které vyučuje aktuální lektor.
        var courses = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e =>
                e.UserId == studentId &&
                e.Status == CourseEnrollmentStatus.Registered &&
                e.Course.CourseInstructors.Any(ci =>
                    ci.InstructorId == lecturerId
                )
            )
            .OrderBy(e => e.Course.StartDate)
            .Select(e => new
            {
                CourseId = e.CourseId,
                CourseName = e.Course.Name,
                e.Course.StartDate,
                e.Course.EndDate,
                e.DanceRole,
                e.Rotation
            })
            .ToListAsync();

        return Ok(new
        {
            Student = student,
            Courses = courses
        });
    }

}
