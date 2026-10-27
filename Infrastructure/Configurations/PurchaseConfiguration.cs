using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Subtotal).HasPrecision(10, 2);
        builder.Property(e => e.FeeAmount).HasPrecision(10, 2);
        builder.Property(e => e.Total).HasPrecision(10, 2);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);

        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Showtime>().WithMany().HasForeignKey(e => e.ShowtimeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Fee>().WithMany().HasForeignKey(e => e.FeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(e => e.Tickets).WithOne().HasForeignKey(t => t.PurchaseId).OnDelete(DeleteBehavior.Restrict);

        // "Mis compras" y el listado de ventas por fecha.
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);

        builder.HasQueryFilter(e => e.IsActive);
    }
}
