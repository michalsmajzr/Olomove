namespace Olomove.Models;

public class CourseSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public DayOfWeek Weekday { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }
}
