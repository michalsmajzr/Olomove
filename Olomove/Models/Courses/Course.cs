using Olomove.Models.Users;

namespace Olomove.Models.Courses;

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public required string CourseType { get; set; }

    public ICollection<CourseDanceStyle> CourseDanceStyles { get; set; } = new List<CourseDanceStyle>();

    public int Level { get; set; }

    public ICollection<CourseInstructor> CourseInstructors { get; set; } = new List<CourseInstructor>();

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int Capacity { get; set; }

    // Zda se má kontrolovat vyváženost Leader / Follower.
    public bool BalancedRoles { get; set; }

    // Maximální povolený rozdíl mezi počtem Leader a Follower.
    public int MaxRoleDifference { get; set; } = 2;

    // Povolení registrace jako Leader.
    public bool AllowLeaderRegistration { get; set; } = true;

    // Povolení registrace jako Follower.
    public bool AllowFollowerRegistration { get; set; } = true;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CourseSession> Sessions { get; set; } = new List<CourseSession>();

    // Registrovaní uživatelé.
    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();

    // Studenti bez uživatelského účtu.
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
