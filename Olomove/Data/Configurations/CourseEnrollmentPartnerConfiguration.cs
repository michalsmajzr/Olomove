
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Olomove.Models.Courses;

namespace Olomove.Data.Configurations;

public class CourseEnrollmentPartnerConfiguration
    : IEntityTypeConfiguration<CourseEnrollmentPartner>
{
    public void Configure(
        EntityTypeBuilder<CourseEnrollmentPartner> builder)
    {
        builder.HasKey(p => p.Id);

        // Připojení partnera ke konkrétní přihlášce.
        builder.HasOne(p => p.Enrollment)
            .WithMany()
            .HasForeignKey(p => p.CourseEnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Jedna přihláška může mít maximálně jednoho partnera.
        builder.HasIndex(p => p.CourseEnrollmentId)
            .IsUnique();

        // Propojení s uloženým partnerem.
        builder.HasOne(p => p.SavedPartner)
            .WithMany()
            .HasForeignKey(p => p.SavedPartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DanceRole)
            .HasMaxLength(16);
    }
}
