using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class ScreenConfiguration : IEntityTypeConfiguration<Screen>
{
    public void Configure(EntityTypeBuilder<Screen> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(50);

        builder.HasQueryFilter(e => e.IsActive);
    }
}
