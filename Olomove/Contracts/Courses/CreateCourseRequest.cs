namespace Olomove.Contracts.Courses;

public sealed class CreateCourseRequest
{
    public required string Name { get; init; }

    public required string CourseType { get; init; }

    public required string DanceStyle { get; init; }

    public int Level { get; init; }

    public Guid? InstructorId { get; init; }

    public required string Room { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public int Capacity { get; init; }

    public bool BalancedRoles { get; init; }

    public decimal Price { get; init; }

    public string? Description { get; init; }

    public required List<CreateCourseSessionRequest> Sessions { get; init; }
}

public sealed class CreateCourseSessionRequest
{
    public required string Weekday { get; init; }

    public TimeOnly StartsAt { get; init; }

    public TimeOnly EndsAt { get; init; }
}
