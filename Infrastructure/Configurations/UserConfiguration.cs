using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(150);
        builder.HasIndex(e => e.Email).IsUnique();   // evita emails duplicados
        builder.Property(e => e.Password).IsRequired();
        builder.Property(e => e.Role).HasMaxLength(30);

        builder.HasQueryFilter(e => e.IsActive);
    }
}
