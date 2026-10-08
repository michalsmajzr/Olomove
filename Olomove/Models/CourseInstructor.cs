namespace Olomove.Models;

public class CourseInstructor
{
    public Guid CourseId { get; set; }

    public Course? Course { get; set; }

    public Guid InstructorId { get; set; }

    public User? Instructor { get; set; }
}
