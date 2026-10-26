
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Users;
using Olomove.Models.Courses;

namespace Olomove.Controllers.Enrollment;

[ApiController]
[Authorize]
[Route("api/courses/{courseId:guid}/enrollments")]
public class CourseEnrollmentsController(
    ApplicationDbContext context,
    UserManager<User> userManager
) : ControllerBase
{
    public class EnrollRequest
    {
        [Required]
        [RegularExpression(
            "^(Leader|Follower)$",
            ErrorMessage = "Taneční role musí být Leader nebo Follower."
        )]
        public string DanceRole { get; set; } = null!;

        public bool Rotation { get; set; } = true;
    }

    // POST: api/courses/{courseId}/enrollments
    [HttpPost]
    public async Task<IActionResult> Enroll(
        Guid courseId,
        [FromBody] EnrollRequest request)
    {
        var userIdText = userManager.GetUserId(User);

        if (!Guid.TryParse(userIdText, out var userId))
            return Unauthorized();

        // Transakce zabrání souběžnému překročení kapacity.
        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable
            );

        try
        {
            var course = await context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Kurz nebyl nalezen.");

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (course.EndDate < today)
                return BadRequest("Na ukončený kurz se nelze přihlásit.");

            var existing = await context.CourseEnrollments
                .FirstOrDefaultAsync(e =>
                    e.CourseId == courseId &&
                    e.UserId == userId
                );

            if (existing != null &&
                existing.Status == CourseEnrollmentStatus.Registered)
            {
                return Conflict(
                    "Na tento kurz jste již přihlášen."
                );
            }

            if (existing != null &&
                existing.Status == CourseEnrollmentStatus.WaitingList)
            {
                return Conflict(
                    "Na tento kurz jste již zapsán na čekací listině."
                );
            }

            var occupied = await context.CourseEnrollments
                .CountAsync(e =>
                    e.CourseId == courseId &&
                    e.Status == CourseEnrollmentStatus.Registered
                );

            if (occupied >= course.Capacity)
                return Conflict("Kapacita kurzu je naplněna.");

            CourseEnrollment enrollment;

            if (existing != null)
            {
                // Obnovení dříve zrušené přihlášky.
                enrollment = existing;
                enrollment.Status = CourseEnrollmentStatus.Registered;
                enrollment.DanceRole = request.DanceRole;
                enrollment.Rotation = request.Rotation;
            }
            else
            {
                enrollment = new CourseEnrollment
                {
                    CourseId = courseId,
                    UserId = userId,
                    DanceRole = request.DanceRole,
                    Rotation = request.Rotation,
                    Status = CourseEnrollmentStatus.Registered
                };

                context.CourseEnrollments.Add(enrollment);
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                enrollment.Id,
                enrollment.CourseId,
                enrollment.UserId,
                enrollment.DanceRole,
                enrollment.Rotation,
                enrollment.Status
            });
        }
        catch (Exception ex) when (
            ex is DbUpdateException ||
            ex is Npgsql.PostgresException pg &&
            pg.SqlState == "40001"
        )
        {
            await transaction.RollbackAsync();

            // Při souběžných požadavcích může PostgreSQL
            // odmítnout jednu transakci.
            return Conflict(
                "Přihlášku se nepodařilo dokončit kvůli souběžné změně. Zkuste to znovu."
            );
        }
    }
}
