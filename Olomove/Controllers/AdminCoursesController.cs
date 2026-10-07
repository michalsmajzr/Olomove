using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Olomove.Contracts.Courses;
using Olomove.Data;
using Olomove.Models;
namespace Olomove.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/courses")]
public class AdminCoursesController(
    ApplicationDbContext dbContext,
    UserManager<User> userManager) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request)
    {
        Validate(request);
        User? instructor = null;

        if (request.InstructorId.HasValue)
        {
            instructor = await userManager.FindByIdAsync(
                request.InstructorId.Value.ToString());

            if (instructor is null)
            {
                ModelState.AddModelError(
                    nameof(request.InstructorId),
                    "Vybraný lektor neexistuje.");
            }
            else if (!await userManager.IsInRoleAsync(instructor, "Lecturer")
                     && !await userManager.IsInRoleAsync(instructor, "Teacher"))
            {
                ModelState.AddModelError(
                    nameof(request.InstructorId),
                    "Vybraný uživatel nemá roli lektora.");
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
            DanceStyle = request.DanceStyle.Trim(),
            Level = request.Level,
            InstructorId = instructor?.Id,
            Room = request.Room.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Capacity = request.Capacity,
            BalancedRoles = request.BalancedRoles,
            Price = request.Price,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            Sessions = request.Sessions.Select(session => new CourseSession
            {
                Weekday = Enum.Parse<DayOfWeek>(
                    session.Weekday,
                    ignoreCase: true),
                StartsAt = session.StartsAt,
                EndsAt = session.EndsAt
            }).ToList()
        };

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync();

        return Created($"/api/admin/courses/{course.Id}", new { course.Id });
    }

    private void Validate(CreateCourseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Název kurzu je povinný.");
        }

        if (string.IsNullOrWhiteSpace(request.CourseType))
        {
            ModelState.AddModelError(nameof(request.CourseType), "Typ kurzu je povinný.");
        }

        if (string.IsNullOrWhiteSpace(request.DanceStyle))
        {
            ModelState.AddModelError(nameof(request.DanceStyle), "Styl tance je povinný.");
        }

        if (request.Level is < 1 or > 8)
        {
            ModelState.AddModelError(nameof(request.Level), "Úroveň musí být mezi 1 a 8.");
        }

        if (string.IsNullOrWhiteSpace(request.Room))
        {
            ModelState.AddModelError(nameof(request.Room), "Sál je povinný.");
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
            ModelState.AddModelError(nameof(request.Sessions), "Kurz musí mít alespoň jeden termín.");
        }
        else if (request.Sessions.Any(session => session.StartsAt >= session.EndsAt))
        {
            ModelState.AddModelError(nameof(request.Sessions), "Čas konce termínu musí být později než čas začátku.");
        }
        else if (request.Sessions.Any(session => !Enum.TryParse<DayOfWeek>(session.Weekday, ignoreCase: true, out _)))
        {
            ModelState.AddModelError(nameof(request.Sessions), "Každý termín musí obsahovat platný den v týdnu.");
        }
    }
}
