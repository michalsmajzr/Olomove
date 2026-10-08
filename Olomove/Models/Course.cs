namespace Olomove.Models;

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

    public bool BalancedRoles { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CourseSession> Sessions { get; set; } = new List<CourseSession>();
}
