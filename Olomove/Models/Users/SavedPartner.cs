using Olomove.Models.Courses;

namespace Olomove.Models.Users;

public class SavedPartner
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Osobní údaje partnera
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    // Uživatel, který si partnera uložil
    public Guid OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    // Datum vytvoření záznamu
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    // Přihlášky, na kterých je tento partner zapsán.
    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
}
