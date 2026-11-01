using Olomove.Models.Courses.Enums;
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
    public DanceRole? DanceRole { get; set; }

    // Zda chce během lekcí střídat taneční partnery.
    public bool Rotation { get; set; } = true;

    // Partner, kterého si uživatel přihlásil.
    public Guid? SavedPartnerId { get; set; }
    public SavedPartner? SavedPartner { get; set; }
    public DanceRole? PartnerDanceRole { get; set; }
    public bool? PartnerRotation { get; set; }

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
