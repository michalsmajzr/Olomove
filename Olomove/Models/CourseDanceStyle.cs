namespace Olomove.Models;

public class CourseDanceStyle
{
    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid DanceStyleId { get; set; }

    public DanceStyle? DanceStyle { get; set; }
}
