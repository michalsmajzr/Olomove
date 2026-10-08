namespace Olomove.Models
{
    public class Room
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Name { get; set; }

        public required int Capacity { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<CourseSession> CourseSessions { get; set; } = new List<CourseSession>();
    }
}
