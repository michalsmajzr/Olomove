using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models;

namespace Olomove.Controllers.admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/courses")]
public class CoursesController(
    ApplicationDbContext dbContext,
    UserManager<User> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await dbContext.Courses
            .AsNoTracking()
            .OrderBy(course => course.Name)
            .Select(course => new
            {
                course.Id,
                course.Name,
                course.Capacity,
                course.Level,
                course.StartDate,
                course.EndDate
            })
            .ToListAsync();

        return Ok(courses);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCourse(Guid id)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.CourseType,
                item.Level,
                item.StartDate,
                item.EndDate,
                item.Capacity,
                item.BalancedRoles,
                item.Price,
                item.Description,
                DanceStyleIds = item.CourseDanceStyles.Select(style => style.DanceStyleId),
                InstructorIds = item.CourseInstructors.Select(instructor => instructor.InstructorId),
                Sessions = item.Sessions.Select(session => new
                {
                    session.RoomId,
                    session.Weekday,
                    session.StartsAt,
                    session.EndsAt
                })
            })
            .FirstOrDefaultAsync();

        return course is null ? NotFound() : Ok(course);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(Course request)
    {
        var danceStyleIds = request.CourseDanceStyles
            .Select(courseDanceStyle => courseDanceStyle.DanceStyleId)
            .Distinct()
            .ToList();
        var danceStyles = await dbContext.DanceStyles
            .Where(danceStyle => danceStyleIds.Contains(danceStyle.Id))
            .ToListAsync();
        var sessionRoomIds = request.Sessions
            .Select(session => session.RoomId)
            .Distinct()
            .ToList();
        var rooms = await dbContext.Rooms
            .Where(room => sessionRoomIds.Contains(room.Id))
            .ToListAsync();

        Validate(request, danceStyleIds, danceStyles, sessionRoomIds, rooms);

        var instructorIds = request.CourseInstructors
            .Select(courseInstructor => courseInstructor.InstructorId)
            .Where(instructorId => instructorId != Guid.Empty)
            .Distinct()
            .ToList();
        var instructors = new List<User>();

        foreach (var instructorId in instructorIds)
        {
            var instructor = await userManager.FindByIdAsync(instructorId.ToString());

            if (instructor is null)
            {
                ModelState.AddModelError(nameof(request.CourseInstructors), "Vybraný lektor neexistuje.");
            }
            else if (!await userManager.IsInRoleAsync(instructor, "Lecturer")
                     && !await userManager.IsInRoleAsync(instructor, "Teacher"))
            {
                ModelState.AddModelError(nameof(request.CourseInstructors), "Vybraný uživatel nemá roli lektora.");
            }
            else
            {
                instructors.Add(instructor);
            }
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var course = new Course
        {
            Name = request.Name.Trim(),
            CourseType = request.CourseType.Trim(),
            Level = request.Level,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Capacity = request.Capacity,
            BalancedRoles = request.BalancedRoles,
            Price = request.Price,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            CourseInstructors = instructors
                .Select(instructor => new CourseInstructor { InstructorId = instructor.Id })
                .ToList(),
            CourseDanceStyles = danceStyleIds
                .Select(danceStyleId => new CourseDanceStyle { DanceStyleId = danceStyleId })
                .ToList(),
            Sessions = request.Sessions
                .Select(session => new CourseSession
                {
                    RoomId = session.RoomId,
                    Weekday = session.Weekday,
                    StartsAt = session.StartsAt,
                    EndsAt = session.EndsAt
                })
                .ToList()
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        return Created($"/api/admin/courses/{course.Id}", new { course.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCourse(Guid id, Course request)
    {
        var course = await dbContext.Courses
            .Include(item => item.CourseDanceStyles)
            .Include(item => item.CourseInstructors)
            .Include(item => item.Sessions)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (course is null)
        {
            return NotFound();
        }

        var danceStyleIds = request.CourseDanceStyles
            .Select(item => item.DanceStyleId)
            .Distinct()
            .ToList();
        var danceStyles = await dbContext.DanceStyles
            .Where(item => danceStyleIds.Contains(item.Id))
            .ToListAsync();
        var sessionRoomIds = request.Sessions
            .Select(item => item.RoomId)
            .Distinct()
            .ToList();
        var rooms = await dbContext.Rooms
            .Where(item => sessionRoomIds.Contains(item.Id))
            .ToListAsync();
        var instructorIds = request.CourseInstructors
            .Select(item => item.InstructorId)
            .Where(item => item != Guid.Empty)
            .Distinct()
            .ToList();
        var instructors = new List<User>();

        Validate(request, danceStyleIds, danceStyles, sessionRoomIds, rooms);

        foreach (var instructorId in instructorIds)
        {
            var instructor = await userManager.FindByIdAsync(instructorId.ToString());

            if (instructor is null)
            {
                ModelState.AddModelError(nameof(request.CourseInstructors), "Vybraný lektor neexistuje.");
            }
            else if (!await userManager.IsInRoleAsync(instructor, "Lecturer")
                     && !await userManager.IsInRoleAsync(instructor, "Teacher"))
            {
                ModelState.AddModelError(nameof(request.CourseInstructors), "Vybraný uživatel nemá roli lektora.");
            }
            else
            {
                instructors.Add(instructor);
            }
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        dbContext.CourseDanceStyles.RemoveRange(course.CourseDanceStyles);
        dbContext.CourseInstructors.RemoveRange(course.CourseInstructors);
        dbContext.CourseSessions.RemoveRange(course.Sessions);

        course.Name = request.Name.Trim();
        course.CourseType = request.CourseType.Trim();
        course.Level = request.Level;
        course.StartDate = request.StartDate;
        course.EndDate = request.EndDate;
        course.Capacity = request.Capacity;
        course.BalancedRoles = request.BalancedRoles;
        course.Price = request.Price;
        course.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await dbContext.SaveChangesAsync();

        var courseDanceStyles = danceStyleIds
            .Select(danceStyleId => new CourseDanceStyle { CourseId = course.Id, DanceStyleId = danceStyleId })
            .ToList();
        var courseInstructors = instructors
            .Select(instructor => new CourseInstructor { CourseId = course.Id, InstructorId = instructor.Id })
            .ToList();
        var courseSessions = request.Sessions
            .Select(session => new CourseSession
            {
                CourseId = course.Id,
                RoomId = session.RoomId,
                Weekday = session.Weekday,
                StartsAt = session.StartsAt,
                EndsAt = session.EndsAt
            })
            .ToList();

        dbContext.CourseDanceStyles.AddRange(courseDanceStyles);
        dbContext.CourseInstructors.AddRange(courseInstructors);
        dbContext.CourseSessions.AddRange(courseSessions);

        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourse(Guid id)
    {
        var course = await dbContext.Courses.FindAsync(id);

        if (course is null)
        {
            return NotFound();
        }

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    private void Validate(
        Course request,
        List<Guid> danceStyleIds,
        List<DanceStyle> danceStyles,
        List<Guid> sessionRoomIds,
        List<Room> rooms)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Název kurzu je povinný.");
        }

        if (string.IsNullOrWhiteSpace(request.CourseType))
        {
            ModelState.AddModelError(nameof(request.CourseType), "Typ kurzu je povinný.");
        }

        if (request.CourseInstructors.Count == 0)
        {
            ModelState.AddModelError(nameof(request.CourseInstructors), "Kurz musí mít alespoň jednoho lektora.");
        }

        if (danceStyleIds.Count == 0)
        {
            ModelState.AddModelError(nameof(request.CourseDanceStyles), "Kurz musí mít alespoň jeden taneční styl.");
        }
        else if (danceStyles.Count != danceStyleIds.Count)
        {
            ModelState.AddModelError(nameof(request.CourseDanceStyles), "Jeden nebo více vybraných tanečních stylů neexistuje.");
        }

        if (request.Level is < 1 or > 8)
        {
            ModelState.AddModelError(nameof(request.Level), "Úroveň musí být mezi 1 a 8.");
        }

        if (request.StartDate == default)
        {
            ModelState.AddModelError(nameof(request.StartDate), "Datum začátku je povinné.");
        }

        if (request.EndDate == default)
        {
            ModelState.AddModelError(nameof(request.EndDate), "Datum konce je povinné.");
        }

        if (request.StartDate != default && request.EndDate != default && request.EndDate < request.StartDate)
        {
            ModelState.AddModelError(nameof(request.EndDate), "Datum konce musí být stejné nebo pozdější než datum začátku.");
        }

        if (request.Capacity < 1)
        {
            ModelState.AddModelError(nameof(request.Capacity), "Kapacita musí být alespoň 1.");
        }
        else if (rooms.Count > 0 && request.Capacity > rooms.Min(room => room.Capacity))
        {
            ModelState.AddModelError(nameof(request.Capacity), $"Kapacita kurzu nesmí překročit kapacitu nejmenšího vybraného sálu ({rooms.Min(room => room.Capacity)} míst).");
        }

        if (request.BalancedRoles && request.Capacity % 2 != 0)
        {
            ModelState.AddModelError(nameof(request.Capacity), "Pro vyvážené role musí být kapacita sudá.");
        }

        if (request.Price < 0)
        {
            ModelState.AddModelError(nameof(request.Price), "Cena nemůže být záporná.");
        }

        if (request.Sessions.Count == 0)
        {
            ModelState.AddModelError(nameof(request.Sessions), "Kurz musí mít alespoň jeden den.");
        }
        else if (sessionRoomIds.Count != rooms.Count)
        {
            ModelState.AddModelError(nameof(request.Sessions), "Jeden nebo více vybraných sálů neexistuje.");
        }
        else if (request.Sessions.Any(session => session.RoomId == Guid.Empty))
        {
            ModelState.AddModelError(nameof(request.Sessions), "Každý den musí mít vybraný sál.");
        }
        else if (request.Sessions.Any(session => session.StartsAt >= session.EndsAt))
        {
            ModelState.AddModelError(nameof(request.Sessions), "Čas konce musí být později než čas začátku.");
        }
    }
}
