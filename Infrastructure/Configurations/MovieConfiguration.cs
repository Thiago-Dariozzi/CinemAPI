using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Synopsis).HasMaxLength(5000);
        builder.Property(e => e.SuggestedPrice).HasPrecision(10, 2);

        builder.HasOne<Genre>().WithMany().HasForeignKey(e => e.GenreId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => e.IsActive);
    }
}
