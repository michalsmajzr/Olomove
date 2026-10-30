
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Olomove.Models.Users;

namespace Olomove.Data.Configurations;

public class SavedPartnerConfiguration : IEntityTypeConfiguration<SavedPartner>
{
    public void Configure(EntityTypeBuilder<SavedPartner> builder)
    {
        // Partner patří uživateli, který si ho uložil.
        builder.HasOne(partner => partner.Owner)
            .WithMany()
            .HasForeignKey(partner => partner.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Omezení délky kontaktních údajů.
        builder.Property(partner => partner.Email)
            .HasMaxLength(256);

        builder.Property(partner => partner.PhoneNumber)
            .HasMaxLength(32);
    }
}
