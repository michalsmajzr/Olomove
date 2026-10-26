using Olomove.Models.Users;

namespace Olomove.Models.Courses;

public class CourseEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Kurz, na který je uživatel přihlášený
    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    // Přihlášený uživatel
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    // Taneční role: Leader / Follower
    public string? DanceRole { get; set; }

    // Zda chce student během výuky střídat partnery
    public bool Rotation { get; set; } = true;

    // Stav přihlášky
    public CourseEnrollmentStatus Status { get; set; }
        = CourseEnrollmentStatus.Registered;

    // Datum přihlášení
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}

public enum CourseEnrollmentStatus
{
    Registered = 0,
    Cancelled = 1,
    WaitingList = 2
}
