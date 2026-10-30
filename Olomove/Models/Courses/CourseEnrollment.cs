using Olomove.Models.Users;

namespace Olomove.Models.Courses;

public class CourseEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Kurz, na který se uživatel přihlašuje.
    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    // Uživatel, který vytvořil a zaplatil přihlášku.
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    // Taneční role: Leader / Follower.
    public string? DanceRole { get; set; }

    // Zda chce během lekcí střídat taneční partnery.
    public bool Rotation { get; set; } = true;

    // Stav přihlášky.
    public CourseEnrollmentStatus Status { get; set; }
        = CourseEnrollmentStatus.Registered;

    // Počet skutečně zaplacených kreditů.
    // Obsahuje i cenu partnera, pokud byl přihlášen.
    public decimal PaidCredits { get; set; } = 0m;

    // Datum vytvoření přihlášky.
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}

public enum CourseEnrollmentStatus
{
    Registered = 0,
    Cancelled = 1,
    WaitingList = 2
}
