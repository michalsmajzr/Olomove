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

    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<Student> Students => Set<Student>();

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

        // Jeden uživatel může mít pouze jednu přihlášku na daný kurz.
        modelBuilder.Entity<CourseEnrollment>()
            .HasIndex(enrollment => new
            {
                enrollment.CourseId,
                enrollment.UserId
            })
            .IsUnique();

        // Vazba přihlášky na kurz.
        modelBuilder.Entity<CourseEnrollment>()
            .HasOne(enrollment => enrollment.Course)
            .WithMany(course => course.CourseEnrollments)
            .HasForeignKey(enrollment => enrollment.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vazba přihlášky na uživatele.
        modelBuilder.Entity<CourseEnrollment>()
            .HasOne(enrollment => enrollment.User)
            .WithMany(user => user.CourseEnrollments)
            .HasForeignKey(enrollment => enrollment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stav přihlášky se v databázi uloží jako text.
        modelBuilder.Entity<CourseEnrollment>()
            .Property(enrollment => enrollment.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Taneční role.
        modelBuilder.Entity<CourseEnrollment>()
            .Property(enrollment => enrollment.DanceRole)
            .HasMaxLength(20);

        // Student patří právě jednomu kurzu.
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Course)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Student může mít jednoho přiřazeného učitele.
        // Učitel je uživatel systému s rolí Teacher.
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Teacher)
            .WithMany()
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}