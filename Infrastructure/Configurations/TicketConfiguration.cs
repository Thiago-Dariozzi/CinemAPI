using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Price).HasPrecision(10, 2);

        builder.HasOne<Showtime>().WithMany().HasForeignKey(e => e.ShowtimeId).OnDelete(DeleteBehavior.Restrict);

        // Una butaca se vende una sola vez por función. Es la última defensa ante dos compras
        // simultáneas que pasan juntas la validación del servicio: la segunda choca acá (409).
        // Las entradas canceladas o dadas de baja liberan la butaca.
        builder.HasIndex(e => new { e.ShowtimeId, e.SeatRow, e.SeatNumber })
            .IsUnique()
            .HasFilter("[IsCancelled] = 0 AND [IsActive] = 1");

        builder.HasQueryFilter(e => e.IsActive);
    }
}
