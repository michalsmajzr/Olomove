using Microsoft.AspNetCore.Identity;
using Olomove.Models.Courses;

namespace Olomove.Models.Users;

public class User : IdentityUser<Guid>
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public decimal Credit { get; set; } = 0;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CourseInstructor> CourseInstructors { get; set; } = new List<CourseInstructor>();

    // Kurzy, na které je uživatel přihlášený jako student.
    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
}
