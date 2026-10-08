namespace Olomove.Models;

public enum CourseWeekday
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}


public class CourseSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid RoomId { get; set; }

    public Room? Room { get; set; }

    public CourseWeekday Weekday { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }
}
