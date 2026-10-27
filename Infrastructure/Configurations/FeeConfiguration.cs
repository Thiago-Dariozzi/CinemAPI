using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class FeeConfiguration : IEntityTypeConfiguration<Fee>
{
    public void Configure(EntityTypeBuilder<Fee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Percentage).HasPrecision(5, 2);
        builder.Property(e => e.FixedAmount).HasPrecision(10, 2);

        // A lo sumo un cargo vigente: el filtro deja que existan muchos no vigentes o dados de baja.
        builder.HasIndex(e => e.IsCurrent).IsUnique().HasFilter("[IsCurrent] = 1 AND [IsActive] = 1");

        builder.HasQueryFilter(e => e.IsActive);
    }
}
