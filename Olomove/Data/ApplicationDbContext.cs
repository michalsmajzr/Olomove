using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Olomove.Models;

namespace Olomove.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseDanceStyle> CourseDanceStyles => Set<CourseDanceStyle>();
    public DbSet<CourseInstructor> CourseInstructors => Set<CourseInstructor>();
    public DbSet<CourseSession> CourseSessions => Set<CourseSession>();
    public DbSet<DanceStyle> DanceStyles => Set<DanceStyle>();
    public DbSet<Room> Rooms => Set<Room>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CourseDanceStyle>()
            .HasKey(courseDanceStyle => new
            {
                courseDanceStyle.CourseId,
                courseDanceStyle.DanceStyleId
            });

        modelBuilder.Entity<CourseDanceStyle>()
            .HasOne(courseDanceStyle => courseDanceStyle.Course)
            .WithMany(course => course.CourseDanceStyles)
            .HasForeignKey(courseDanceStyle => courseDanceStyle.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CourseDanceStyle>()
            .HasOne(courseDanceStyle => courseDanceStyle.DanceStyle)
            .WithMany(danceStyle => danceStyle.CourseDanceStyles)
            .HasForeignKey(courseDanceStyle => courseDanceStyle.DanceStyleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CourseSession>()
            .Property(courseSession => courseSession.Weekday)
            .HasConversion<string>()
            .HasMaxLength(9);

        modelBuilder.Entity<CourseInstructor>()
            .HasKey(courseInstructor => new
            {
                courseInstructor.CourseId,
                courseInstructor.InstructorId
            });

        modelBuilder.Entity<CourseInstructor>()
            .HasOne(courseInstructor => courseInstructor.Course)
            .WithMany(course => course.CourseInstructors)
            .HasForeignKey(courseInstructor => courseInstructor.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CourseInstructor>()
            .HasOne(courseInstructor => courseInstructor.Instructor)
            .WithMany(user => user.CourseInstructors)
            .HasForeignKey(courseInstructor => courseInstructor.InstructorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}