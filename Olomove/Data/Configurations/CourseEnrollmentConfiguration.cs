using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Olomove.Models.Courses;

namespace Olomove.Data.Configurations;

public class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> builder)
    {
        builder.HasKey(e => e.Id);

        // Vztah s hlavním uživatelem
        builder.HasOne(e => e.User)
            .WithMany(u => u.CourseEnrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vztah s kurzem
        builder.HasOne(e => e.Course)
            .WithMany(c => c.CourseEnrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vazba na uloženého partnera
        builder.HasOne(e => e.SavedPartner)
            .WithMany(p => p.CourseEnrollments)
            .HasForeignKey(e => e.SavedPartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Konfigurace DanceRole a PartnerDanceRole jako stringy s maximální délkou 16 znaků
        builder.Property(e => e.DanceRole)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.PartnerDanceRole)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}