using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Price).HasPrecision(10, 2);

        builder.HasOne<Movie>().WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Screen>().WithMany().HasForeignKey(e => e.ScreenId).OnDelete(DeleteBehavior.Restrict);

        // Lo usan la cartelera por sala y día y la validación de solapamiento.
        builder.HasIndex(e => new { e.ScreenId, e.StartTime });

        builder.HasQueryFilter(e => e.IsActive);
    }
}
