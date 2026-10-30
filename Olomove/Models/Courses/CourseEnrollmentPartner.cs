
using Olomove.Models.Users;

namespace Olomove.Models.Courses;

public class CourseEnrollmentPartner
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Přihláška, ke které partner patří.
    public Guid CourseEnrollmentId { get; set; }

    public CourseEnrollment Enrollment { get; set; } = null!;

    // Odkaz na uloženého partnera.
    public Guid SavedPartnerId { get; set; }

    public SavedPartner SavedPartner { get; set; } = null!;

    // Jméno v době přihlášení na kurz.
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    // Leader nebo Follower.
    public string? DanceRole { get; set; }

    // Zda chce partner během kurzu střídat.
    public bool Rotation { get; set; } = true;
}
