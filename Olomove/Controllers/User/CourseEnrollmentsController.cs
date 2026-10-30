
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Olomove.Constants;
using Olomove.Data;
using Olomove.Models.Courses;
using Olomove.Models.Users;

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
        public string? DanceRole { get; set; }
        public bool Rotation { get; set; } = true;

        // Volitelné: uživatel se může přihlásit sám.
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

        var registered = await context.CourseEnrollments
            .CountAsync(e =>
                e.CourseId == courseId &&
                e.Status == CourseEnrollmentStatus.Registered);

        var partners = await context.CourseEnrollmentPartners
            .CountAsync(p =>
                p.Enrollment.CourseId == courseId &&
                p.Enrollment.Status == CourseEnrollmentStatus.Registered);

        var students = await context.Students
            .CountAsync(s => s.CourseId == courseId);

        var occupied = registered + partners + students;

        // BalancedRoles je zdroj pravdy pro parovy kurz.
        // Do poctu roli patri i partneri bez uzivatelskeho uctu.
        var leaders = await context.CourseEnrollments.CountAsync(e =>
            e.CourseId == courseId &&
            e.Status == CourseEnrollmentStatus.Registered &&
            e.DanceRole == "Leader");

        leaders += await context.CourseEnrollmentPartners.CountAsync(p =>
            p.Enrollment.CourseId == courseId &&
            p.Enrollment.Status == CourseEnrollmentStatus.Registered &&
            p.DanceRole == "Leader");

        var followers = await context.CourseEnrollments.CountAsync(e =>
            e.CourseId == courseId &&
            e.Status == CourseEnrollmentStatus.Registered &&
            e.DanceRole == "Follower");

        followers += await context.CourseEnrollmentPartners.CountAsync(p =>
            p.Enrollment.CourseId == courseId &&
            p.Enrollment.Status == CourseEnrollmentStatus.Registered &&
            p.DanceRole == "Follower");

        var maxPerRole = course.Capacity / 2;
        var hasFreePlace = occupied < course.Capacity;
        var canRegisterLeader = course.BalancedRoles &&
            course.AllowLeaderRegistration &&
            leaders < maxPerRole && hasFreePlace;
        var canRegisterFollower = course.BalancedRoles &&
            course.AllowFollowerRegistration &&
            followers < maxPerRole && hasFreePlace;
        // Pokud lektor uzavre samostatne prihlasovani jedne role,
        // cely par je nadale povoleny, pokud se vejde do obou limitu.
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
    [Authorize(Roles = UserRoles.User)] // Přihlášky smí vytvářet pouze zákazníci.
    public async Task<IActionResult> Enroll(
        Guid courseId,
        [FromBody] EnrollRequest request)
    {
        var userIdText = userManager.GetUserId(User);

        if (!Guid.TryParse(userIdText, out var userId))
            return Unauthorized();

        // Kontrola a odečtení kreditů budou v jedné transakci.
        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var course = await context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Kurz nebyl nalezen.");

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (course.StartDate < today)
                return BadRequest(
                    "Na kurz, který již začal, se nelze přihlásit.");

            if (today > course.StartDate.AddDays(-7))
                {
                    return BadRequest(
                        "Přihlášení je možné nejpozději 7 dní před začátkem kurzu.");
                }

            if (course.Price < 0 || course.Capacity < 1)
                return BadRequest("Kurz nemá platnou cenu nebo kapacitu.");

            var isSolo = !course.BalancedRoles;
            var role = request.DanceRole?.Trim();

            if (isSolo)
            {
                if (request.SavedPartnerId.HasValue)
                    return BadRequest(
                        "Na sólový kurz nelze přihlásit partnera.");

                role = null;
            }
            else if (role is not ("Leader" or "Follower"))
            {
                return BadRequest(
                    "Vyberte taneční roli Leader nebo Follower.");
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
                    return BadRequest(
                        "Vybraný partner neexistuje nebo vám nepatří.");
            }

            // Samostatná přihláška musí respektovat povolené role.
            // Kompletní pár přihlašuje Leader + Follower současně.
            if (!isSolo && partner == null)
            {
                if (role == "Leader" &&
                    !course.AllowLeaderRegistration)
                    return Conflict(
                        "Samostatné přihlašování role Leader je uzavřené.");

                if (role == "Follower" &&
                    !course.AllowFollowerRegistration)
                    return Conflict(
                        "Samostatné přihlašování role Follower je uzavřené.");
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

            // Počet účastníků = uživatelé + partneři + Students.
            // Students pouze čteme, nijak je neměníme.
            var registered = await context.CourseEnrollments
                .CountAsync(e =>
                    e.CourseId == courseId &&
                    e.Status == CourseEnrollmentStatus.Registered);

            var registeredPartners =
                await context.CourseEnrollmentPartners
                    .CountAsync(p =>
                        p.Enrollment.CourseId == courseId &&
                        p.Enrollment.Status ==
                            CourseEnrollmentStatus.Registered);

            var students = await context.Students
                .CountAsync(s => s.CourseId == courseId);

            var peopleCount = partner == null ? 1 : 2;

            if (registered + registeredPartners + students +
                peopleCount > course.Capacity)
            {
                return Conflict(
                    "Na kurzu není dostatek volných míst.");
            }

            // Pevna kvota: maximalne polovina kapacity pro kazdou roli.
            // Kontrolujeme i partnery bez uctu, a to i u prihlaseni celeho paru.
            if (!isSolo)
            {
                var maxPerRole = course.Capacity / 2;

                var leaderCount = await context.CourseEnrollments.CountAsync(e =>
                    e.CourseId == courseId &&
                    e.Status == CourseEnrollmentStatus.Registered &&
                    e.DanceRole == "Leader");

                leaderCount += await context.CourseEnrollmentPartners.CountAsync(p =>
                    p.Enrollment.CourseId == courseId &&
                    p.Enrollment.Status == CourseEnrollmentStatus.Registered &&
                    p.DanceRole == "Leader");

                var followerCount = await context.CourseEnrollments.CountAsync(e =>
                    e.CourseId == courseId &&
                    e.Status == CourseEnrollmentStatus.Registered &&
                    e.DanceRole == "Follower");

                followerCount += await context.CourseEnrollmentPartners.CountAsync(p =>
                    p.Enrollment.CourseId == courseId &&
                    p.Enrollment.Status == CourseEnrollmentStatus.Registered &&
                    p.DanceRole == "Follower");

                var addingLeader = partner != null || role == "Leader" ? 1 : 0;
                var addingFollower = partner != null || role == "Follower" ? 1 : 0;

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
                    $"Nedostatek kreditů. Potřebujete {totalPrice}, " +
                    $"ale máte pouze {user.Credit}.");
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

            if (existing == null)
                context.CourseEnrollments.Add(enrollment);

            // Případný partner předchozí zrušené přihlášky.
            var oldPartner = existing == null
                ? null
                : await context.CourseEnrollmentPartners
                    .FirstOrDefaultAsync(p =>
                        p.CourseEnrollmentId == existing.Id);

            if (partner == null)
            {
                if (oldPartner != null)
                    context.CourseEnrollmentPartners.Remove(oldPartner);
            }
            else
            {
                var newPartner = oldPartner ??
                    new CourseEnrollmentPartner
                    {
                        Enrollment = enrollment,
                        SavedPartnerId = partner.Id,
                        FirstName = partner.FirstName,
                        LastName = partner.LastName
                    };

                newPartner.SavedPartnerId = partner.Id;
                newPartner.FirstName = partner.FirstName;
                newPartner.LastName = partner.LastName;

                // Partner má opačnou taneční roli.
                newPartner.DanceRole =
                    role == "Leader" ? "Follower" : "Leader";

                newPartner.Rotation = request.PartnerRotation;

                if (oldPartner == null)
                    context.CourseEnrollmentPartners.Add(newPartner);
            }

            await context.SaveChangesAsync();

            // Teprve teď je přihláška i platba potvrzena.
            await transaction.CommitAsync();

            return Ok(new
            {
                enrollment.Id,
                enrollment.CourseId,
                enrollment.Status,
                enrollment.PaidCredits,
                PartnerName = partner == null
                    ? null
                    : $"{partner.FirstName} {partner.LastName}",
                Credit = user.Credit
            });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return Conflict(
                "Přihlášku se nepodařilo dokončit. Zkuste to znovu.");
        }
        catch (PostgresException ex)
            when (ex.SqlState is "40001" or "40P01")
        {
            await transaction.RollbackAsync();

            return Conflict(
                "Došlo k souběžné změně přihlášek. Zkuste to znovu.");
        }
    }

    // POST: api/courses/{courseId}/enrollments/cancel
    [HttpPost("cancel")]
    [Authorize(Roles = UserRoles.User)] // Odhlášení může provést vlastník zákaznického účtu.
    public async Task<IActionResult> Cancel(Guid courseId)
    {
        var userIdText = userManager.GetUserId(User);

        if (!Guid.TryParse(userIdText, out var userId))
            return Unauthorized();

        await using var transaction =
            await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

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
                    "Bezplatné odhlášení je možné nejpozději " +
                    "7 dní před začátkem kurzu.");
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

            return Conflict(
                "Odhlášení se nepodařilo dokončit.");
        }
        catch (PostgresException ex)
            when (ex.SqlState is "40001" or "40P01")
        {
            await transaction.RollbackAsync();

            return Conflict(
                "Proběhla souběžná změna. Zkuste to znovu.");
        }
    }

    
}
