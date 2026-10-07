namespace Olomove.Models;

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public required string CourseType { get; set; }

    public required string DanceStyle { get; set; }

    public int Level { get; set; }

    public Guid? InstructorId { get; set; }

    public User? Instructor { get; set; }

    public required string Room { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int Capacity { get; set; }

    public bool BalancedRoles { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CourseSession> Sessions { get; set; } = new List<CourseSession>();
}
