using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Users;
using Olomove.Models.Courses.Enums;

namespace Olomove.Controllers.Lecturer;

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

    // GET: api/lecturer/courses/{id}/participants
    [HttpGet("{id:guid}/participants")]
    public async Task<IActionResult> GetCourseParticipants(Guid id)
    {
        var userId = userManager.GetUserId(User);
        if (!Guid.TryParse(userId, out var lecturerId))
            return Unauthorized();

        // Lektor smí vidět pouze své kurzy.
        var course = await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id &&
                c.CourseInstructors.Any(ci => ci.InstructorId == lecturerId))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Capacity,
                c.BalancedRoles,
                c.AllowLeaderRegistration,
                c.AllowFollowerRegistration
            })
            .FirstOrDefaultAsync();

        if (course == null)
            return NotFound("Kurz nebyl nalezen nebo k němu nemáte přístup.");

        // Načteme všechny registrace na tento kurz
        var rawEnrollments = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.CourseId == id && e.Status == CourseEnrollmentStatus.Registered)
            .Select(e => new
            {
                e.Id,
                e.UserId,
                UserFirstName = e.User.FirstName,
                UserLastName = e.User.LastName,
                e.User.Email,
                e.User.PhoneNumber,
                e.DanceRole,
                e.Rotation,
                e.CreatedAt,

                // Informace o partnerovi přímo z CourseEnrollment
                e.SavedPartnerId,
                PartnerFirstName = e.SavedPartner != null ? e.SavedPartner.FirstName : null,
                PartnerLastName = e.SavedPartner != null ? e.SavedPartner.LastName : null,
                PartnerEmail = e.SavedPartner != null ? e.SavedPartner.Email : null,
                PartnerPhone = e.SavedPartner != null ? e.SavedPartner.PhoneNumber : null,
                e.PartnerDanceRole,
                e.PartnerRotation
            })
            .ToListAsync();

        // 1) Hlavní uživatelé přihlášení na kurz
        var enrollments = rawEnrollments.Select(e => new
        {
            EnrollmentId = (Guid?)e.Id,
            ParticipantId = e.UserId,
            PersonType = "User",
            FirstName = e.UserFirstName,
            LastName = e.UserLastName,
            FullName = e.UserFirstName + " " + e.UserLastName,
            Email = e.Email,
            PhoneNumber = e.PhoneNumber,
            DanceRole = e.DanceRole,
            Rotation = (bool?)e.Rotation,
            RegisteredAt = (DateTimeOffset?)e.CreatedAt,
            DateOfBirth = (DateOnly?)null,
            TeacherName = (string?)null,
            RegisteredWith = e.SavedPartnerId.HasValue
                ? e.PartnerFirstName + " " + e.PartnerLastName
                : null
        }).ToList();

        // 2) Partneři přihlášení společně s hlavním uživatelem
        var partners = rawEnrollments
            .Where(e => e.SavedPartnerId.HasValue)
            .Select(e => new
            {
                EnrollmentId = (Guid?)e.Id,
                ParticipantId = e.SavedPartnerId!.Value,
                PersonType = "Partner",
                FirstName = e.PartnerFirstName!,
                LastName = e.PartnerLastName!,
                FullName = e.PartnerFirstName + " " + e.PartnerLastName,
                Email = e.PartnerEmail,
                PhoneNumber = e.PartnerPhone,
                DanceRole = e.PartnerDanceRole,
                Rotation = e.PartnerRotation,
                RegisteredAt = (DateTimeOffset?)e.CreatedAt,
                DateOfBirth = (DateOnly?)null,
                TeacherName = (string?)null,
                RegisteredWith = (string?)(e.UserFirstName + " " + e.UserLastName)
            }).ToList();

        // 3) Stávající školní studenti
        var students = await context.Students
            .AsNoTracking()
            .Where(s => s.CourseId == id)
            .Select(s => new
            {
                EnrollmentId = (Guid?)null,
                ParticipantId = s.Id,
                PersonType = "Student",
                s.FirstName,
                s.LastName,
                FullName = s.FirstName + " " + s.LastName,
                Email = (string?)null,
                s.PhoneNumber,
                DanceRole = (DanceRole?)null,
                Rotation = (bool?)null,
                RegisteredAt = (DateTimeOffset?)null,
                s.DateOfBirth,
                TeacherName = s.Teacher != null
                    ? s.Teacher.FirstName + " " + s.Teacher.LastName
                    : null,
                RegisteredWith = (string?)null
            })
            .ToListAsync();

        var participants = enrollments
            .Concat(partners)
            .Concat(students)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToList();

        return Ok(new
        {
            CourseId = course.Id,
            CourseName = course.Name,
            course.Capacity,
            course.BalancedRoles,
            course.AllowLeaderRegistration,
            course.AllowFollowerRegistration,
            RegisteredCount = enrollments.Count,
            PartnerCount = partners.Count,
            StudentCount = students.Count,
            LeaderCount = participants.Count(p => p.DanceRole == DanceRole.Leader),
            FollowerCount = participants.Count(p => p.DanceRole == DanceRole.Follower),
            UnassignedRoleCount = participants.Count(p =>
                p.PersonType != "Student" && p.DanceRole == null),
            TotalCount = participants.Count,
            OccupiedPlaces = participants.Count,
            AvailablePlaces = Math.Max(0, course.Capacity - participants.Count),
            IsFull = participants.Count >= course.Capacity,
            OverCapacity = Math.Max(0, participants.Count - course.Capacity),
            Participants = participants
        });
    }

    // GET: api/lecturer/courses/{id}/participants/{participantId}
    [HttpGet("{id:guid}/participants/{participantId:guid}")]
    public async Task<IActionResult> GetCourseParticipantDetail(
        Guid id,
        Guid participantId)
    {
        var userId = userManager.GetUserId(User);

        if (!Guid.TryParse(userId, out var lecturerId))
            return Unauthorized();

        // Kontrola přístupu ke kurzu.
        var hasAccess = await context.Courses
            .AsNoTracking()
            .AnyAsync(c =>
                c.Id == id &&
                c.CourseInstructors.Any(ci =>
                    ci.InstructorId == lecturerId));

        if (!hasAccess)
            return NotFound("Kurz nebyl nalezen nebo k němu nemáte přístup.");

        // 1. Samostatný školní student
        var student = await context.Students
            .AsNoTracking()
            .Where(s => s.Id == participantId && s.CourseId == id)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                FullName = s.FirstName + " " + s.LastName,
                s.DateOfBirth,
                s.PhoneNumber,
                s.GuardianName,
                s.GuardianEmail,
                s.GuardianPhone,
                s.TeacherId,
                TeacherName = s.Teacher != null
                    ? s.Teacher.FirstName + " " + s.Teacher.LastName
                    : null,
                s.CourseId,
                CourseName = s.Course.Name,
                StartDate = s.Course.StartDate,
                EndDate = s.Course.EndDate,
                s.Note,
                s.CreatedAt,
                s.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (student != null)
        {
            return Ok(new
            {
                PersonType = "Student",
                Participant = student,
                Courses = new[]
                {
                    new
                    {
                        student.CourseId,
                        student.CourseName,
                        student.StartDate,
                        student.EndDate
                    }
                }
            });
        }

        // 2. Partner bez vlastního účtu (vyhledáváme podle SavedPartnerId)
        var partnerEnrollment = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e => e.CourseId == id &&
                        e.SavedPartnerId == participantId &&
                        e.Status == CourseEnrollmentStatus.Registered)
            .Select(e => new
            {
                Id = e.SavedPartnerId,
                SavedPartnerId = e.SavedPartnerId,
                FirstName = e.SavedPartner!.FirstName,
                LastName = e.SavedPartner.LastName,
                FullName = e.SavedPartner.FirstName + " " + e.SavedPartner.LastName,
                Email = e.SavedPartner.Email,
                PhoneNumber = e.SavedPartner.PhoneNumber,
                DanceRole = e.PartnerDanceRole,
                Rotation = e.PartnerRotation,
                RegisteredAt = e.CreatedAt,
                RegisteredWith = e.User.FirstName + " " + e.User.LastName,
                e.CourseId,
                CourseName = e.Course.Name,
                e.Course.StartDate,
                e.Course.EndDate
            })
            .FirstOrDefaultAsync();

        if (partnerEnrollment != null)
        {
            return Ok(new
            {
                PersonType = "Partner",
                Participant = partnerEnrollment,
                Courses = new[]
                {
                    new
                    {
                        partnerEnrollment.CourseId,
                        partnerEnrollment.CourseName,
                        partnerEnrollment.StartDate,
                        partnerEnrollment.EndDate
                    }
                }
            });
        }

        // 3. Registrovaný uživatel (UserId)
        var enrollment = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e =>
                e.CourseId == id &&
                e.UserId == participantId &&
                e.Status == CourseEnrollmentStatus.Registered)
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

        if (enrollment == null)
            return NotFound("Účastník nebyl nalezen.");

        // Další kurzy uživatele vyučované aktuálním lektorem
        var courses = await context.CourseEnrollments
            .AsNoTracking()
            .Where(e =>
                e.UserId == participantId &&
                e.Status == CourseEnrollmentStatus.Registered &&
                e.Course.CourseInstructors.Any(ci =>
                    ci.InstructorId == lecturerId))
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
            PersonType = "User",
            Participant = enrollment,
            Courses = courses
        });
    }
}