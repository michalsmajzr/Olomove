namespace Olomove.Models
{
    public class DanceStyle
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Name { get; set; }

        public string? Genre { get; set; }

        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
