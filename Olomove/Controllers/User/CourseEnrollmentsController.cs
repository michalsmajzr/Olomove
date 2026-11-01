using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Olomove.Constants;
using Olomove.Data;
using Olomove.Models.Courses;
using Olomove.Models.Courses.Enums;
using Olomove.Models.Users;

namespace Olomove.Controllers.Enrollment;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/courses/{courseId:guid}/enrollments")]
public class CourseEnrollmentsController(
    ApplicationDbContext context,
    UserManager<User> userManager
) : ControllerBase
{
    public class EnrollRequest
    {
        public string? DanceRole { get; set; }
        public bool Rotation { get; set; } = true;

        // Volitelné: uživatel může přihlásit partnera.
        public Guid? SavedPartnerId { get; set; }
        public bool PartnerRotation { get; set; } = false;
    }

    // GET: informace pro přihlašovací formulář.
    [HttpGet("preview")]
    [Authorize] // Náhled je dostupný všem přihlášeným účtům.
    public async Task<IActionResult> Preview(Guid courseId)
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            return NotFound("Kurz nebyl nalezen.");

        var activeEnrollments = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId && e.Status == CourseEnrollmentStatus.Registered)
            .Select(e => new
            {
                e.DanceRole,
                e.SavedPartnerId,
                e.PartnerDanceRole
            })
            .ToListAsync();

        var registered = activeEnrollments.Count;
        var partners = activeEnrollments.Count(e => e.SavedPartnerId.HasValue);
        var students = await context.Students.CountAsync(s => s.CourseId == courseId);

        var occupied = registered + partners + students;

        // Do počtu rolí započítáváme jak hlavní uživatele, tak přihlášené partnery.
        var leaders = activeEnrollments.Count(e => e.DanceRole == DanceRole.Leader) +
                      activeEnrollments.Count(e => e.PartnerDanceRole == DanceRole.Leader);

        var followers = activeEnrollments.Count(e => e.DanceRole == DanceRole.Follower) +
                        activeEnrollments.Count(e => e.PartnerDanceRole == DanceRole.Follower);

        var maxPerRole = course.Capacity / 2;
        var hasFreePlace = occupied < course.Capacity;
        var canRegisterLeader = course.BalancedRoles &&
            course.AllowLeaderRegistration &&
            leaders < maxPerRole && hasFreePlace;
        var canRegisterFollower = course.BalancedRoles &&
            course.AllowFollowerRegistration &&
            followers < maxPerRole && hasFreePlace;

        // Pokud lektor uzavře samostatné přihlašování jedné role,
        // celý pár je nadále povolený, pokud se vejde do obou limitů.
        var canRegisterPair = course.BalancedRoles &&
            course.Capacity - occupied >= 2 &&
            leaders < maxPerRole && followers < maxPerRole;

        var existing = await context.CourseEnrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.CourseId == courseId &&
                e.UserId == user.Id);

        var today = DateOnly.FromDateTime(DateTime.Today);

        return Ok(new
        {
            course.Id,
            course.Name,
            course.CourseType,
            course.StartDate,
            course.EndDate,
            course.Price,
            course.Capacity,
            course.BalancedRoles,
            course.AllowLeaderRegistration,
            course.AllowFollowerRegistration,
            LeaderCount = leaders,
            FollowerCount = followers,
            MaxPerRole = maxPerRole,
            CanRegisterLeader = canRegisterLeader,
            CanRegisterFollower = canRegisterFollower,
            CanRegisterPair = canRegisterPair,

            IsSolo = !course.BalancedRoles,
            Occupied = occupied,
            AvailablePlaces = Math.Max(0, course.Capacity - occupied),
            CanRegister = today <= course.StartDate.AddDays(-7),
            MyStatus = existing?.Status.ToString(),
            Credit = user.Credit
        });
    }

    // POST: přihlášení jednotlivce nebo dvojice.
    [HttpPost]
    [Authorize] // Přihlášky smí vytvářet pouze zákazníci.
    public async Task<IActionResult> Enroll(
        Guid courseId,
        [FromBody] EnrollRequest request)
    {
        var userIdText = userManager.GetUserId(User);

        if (!Guid.TryParse(userIdText, out var userId))
            return Unauthorized();

        // Kontrola a odečtení kreditů budou v jedné transakci.
        await using var transaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var course = await context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Kurz nebyl nalezen.");

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (course.StartDate < today)
                return BadRequest("Na kurz, který již začal, se nelze přihlásit.");

            if (today > course.StartDate.AddDays(-7))
                return BadRequest("Přihlášení je možné nejpozději 7 dní před začátkem kurzu.");

            if (course.Price < 0 || course.Capacity < 1)
                return BadRequest("Kurz nemá platnou cenu nebo kapacitu.");

            var isSolo = !course.BalancedRoles;
            DanceRole? role = null;

            if (isSolo)
            {
                if (request.SavedPartnerId.HasValue)
                    return BadRequest("Na sólový kurz nelze přihlásit partnera.");
            }
            else if (!Enum.TryParse<DanceRole>(
                         request.DanceRole?.Trim(),
                         ignoreCase: true,
                         out var parsedRole))
            {
                return BadRequest("Vyberte taneční roli Leader nebo Follower.");
            }
            else
            {
                role = parsedRole;
            }

            // Ověření, že uložený partner patří uživateli.
            SavedPartner? partner = null;

            if (request.SavedPartnerId.HasValue)
            {
                partner = await context.SavedPartners
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p =>
                        p.Id == request.SavedPartnerId.Value &&
                        p.OwnerUserId == userId);

                if (partner == null)
                    return BadRequest("Vybraný partner neexistuje nebo vám nepatří.");
            }

            // Samostatná přihláška musí respektovat povolené role.
            if (!isSolo && partner == null)
            {
                if (role == DanceRole.Leader && !course.AllowLeaderRegistration)
                    return Conflict("Samostatné přihlašování role Leader je uzavřené.");

                if (role == DanceRole.Follower && !course.AllowFollowerRegistration)
                    return Conflict("Samostatné přihlašování role Follower je uzavřené.");
            }

            // Ověření předchozí přihlášky.
            var existing = await context.CourseEnrollments
                .FirstOrDefaultAsync(e =>
                    e.CourseId == courseId &&
                    e.UserId == userId);

            if (existing?.Status == CourseEnrollmentStatus.Registered)
                return Conflict("Na tento kurz už jste přihlášeni.");

            if (existing?.Status == CourseEnrollmentStatus.WaitingList)
                return Conflict("Na tento kurz už jste na čekací listině.");

            // Načtení aktivních přihlášek pro kontrolu kapacity
            var activeEnrollments = await context.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId && e.Status == CourseEnrollmentStatus.Registered)
                .Select(e => new { e.DanceRole, e.SavedPartnerId, e.PartnerDanceRole })
                .ToListAsync();

            var registeredCount = activeEnrollments.Count;
            var registeredPartnersCount = activeEnrollments.Count(e => e.SavedPartnerId.HasValue);
            var students = await context.Students.CountAsync(s => s.CourseId == courseId);

            var peopleCount = partner == null ? 1 : 2;

            if (registeredCount + registeredPartnersCount + students + peopleCount > course.Capacity)
            {
                return Conflict("Na kurzu není dostatek volných míst.");
            }

            // Pevná kvóta: maximálně polovina kapacity pro každou roli.
            if (!isSolo)
            {
                var maxPerRole = course.Capacity / 2;

                var leaderCount = activeEnrollments.Count(e => e.DanceRole == DanceRole.Leader) +
                                  activeEnrollments.Count(e => e.PartnerDanceRole == DanceRole.Leader);

                var followerCount = activeEnrollments.Count(e => e.DanceRole == DanceRole.Follower) +
                                    activeEnrollments.Count(e => e.PartnerDanceRole == DanceRole.Follower);

                var addingLeader = partner != null || role == DanceRole.Leader ? 1 : 0;
                var addingFollower = partner != null || role == DanceRole.Follower ? 1 : 0;

                if (leaderCount + addingLeader > maxPerRole)
                    return Conflict("Kapacita role Leader je naplněna.");

                if (followerCount + addingFollower > maxPerRole)
                    return Conflict("Kapacita role Follower je naplněna.");
            }

            // Kontrola kreditů PŘED vytvořením přihlášky.
            var totalPrice = checked(course.Price * peopleCount);

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized();

            if (user.Credit < totalPrice)
            {
                return Conflict(
                    $"Nedostatek kreditů. Potřebujete {totalPrice}, ale máte pouze {user.Credit}.");
            }

            // Odečtení kreditů.
            user.Credit -= totalPrice;

            // Vytvoření nebo obnovení zrušené přihlášky.
            var enrollment = existing ?? new CourseEnrollment
            {
                CourseId = courseId,
                UserId = userId
            };

            enrollment.DanceRole = role;
            enrollment.Rotation = request.Rotation;
            enrollment.PaidCredits = totalPrice;
            enrollment.Status = CourseEnrollmentStatus.Registered;

            // Nastavení partnera přímo na přihlášku
            if (partner != null)
            {
                enrollment.SavedPartnerId = partner.Id;
                enrollment.PartnerDanceRole = role == DanceRole.Leader ? DanceRole.Follower : DanceRole.Leader;
                enrollment.PartnerRotation = request.PartnerRotation;
            }
            else
            {
                enrollment.SavedPartnerId = null;
                enrollment.PartnerDanceRole = null;
                enrollment.PartnerRotation = null;
            }

            if (existing == null)
                context.CourseEnrollments.Add(enrollment);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                enrollment.Id,
                enrollment.CourseId,
                enrollment.Status,
                enrollment.PaidCredits,
                PartnerName = partner == null ? null : $"{partner.FirstName} {partner.LastName}",
                Credit = user.Credit
            });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return Conflict("Přihlášku se nepodařilo dokončit. Zkuste to znovu.");
        }
        catch (PostgresException ex) when (ex.SqlState is "40001" or "40P01")
        {
            await transaction.RollbackAsync();

            return Conflict("Došlo k souběžné změně přihlášek. Zkuste to znovu.");
        }
    }

    // POST: api/courses/{courseId}/enrollments/cancel
    [HttpPost("cancel")]
    [Authorize(Roles = UserRoles.User)]
    public async Task<IActionResult> Cancel(Guid courseId)
    {
        var userIdText = userManager.GetUserId(User);

        if (!Guid.TryParse(userIdText, out var userId))
            return Unauthorized();

        await using var transaction =
            await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var enrollment = await context.CourseEnrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e =>
                    e.CourseId == courseId &&
                    e.UserId == userId);

            if (enrollment == null)
                return NotFound("Přihláška nebyla nalezena.");

            if (enrollment.Status != CourseEnrollmentStatus.Registered)
                return Conflict("Přihláška již není aktivní.");

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (today > enrollment.Course.StartDate.AddDays(-7))
            {
                return BadRequest(
                    "Bezplatné odhlášení je možné nejpozději 7 dní před začátkem kurzu.");
            }

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Unauthorized();

            // Vrácení původně zaplacené částky.
            user.Credit += enrollment.PaidCredits;
            enrollment.Status = CourseEnrollmentStatus.Cancelled;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                enrollment.Id,
                enrollment.Status,
                Refunded = enrollment.PaidCredits,
                Credit = user.Credit
            });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return Conflict("Odhlášení se nepodařilo dokončit.");
        }
        catch (PostgresException ex) when (ex.SqlState is "40001" or "40P01")
        {
            await transaction.RollbackAsync();

            return Conflict("Proběhla souběžná změna. Zkuste to znovu.");
        }
    }
}