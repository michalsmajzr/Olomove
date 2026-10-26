using Olomove.Models.Courses;

namespace Olomove.Models.Users;

public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Osobní údaje
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    // Nepovinný telefon studenta
    public string? PhoneNumber { get; set; }

    // Kontakt na rodiče / zákonného zástupce
    public string? GuardianName { get; set; }
    public string? GuardianEmail { get; set; }
    public string? GuardianPhone { get; set; }

    // Každý student je evidován v jednom kurzu
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // Učitel s rolí Teacher (nikoliv Instructor)
    public Guid? TeacherId { get; set; }
    public User? Teacher { get; set; }

    // Taneční role
    public string? DanceRole { get; set; }

    // Střídání tanečních partnerů
    public bool Rotation { get; set; } = true;

    // Interní poznámka
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }
}
