using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FinalPrice).HasPrecision(10, 2);

        builder.HasOne<Movie>().WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Screen>().WithMany().HasForeignKey(e => e.ScreenId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => e.IsActive);
    }
}
