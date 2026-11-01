using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(e => e.Id);

        // Collation accent-insensitive + case-insensitive: "Acción", "Accion" y
        // "ACCION" comparan igual también a nivel de base, como última línea de
        // defensa. No cambia cómo se guarda/lee el string, solo cómo se compara.
        builder.Property(e => e.Name).IsRequired().HasMaxLength(50).UseCollation("Modern_Spanish_CI_AI");

        // Filtrado a IsActive = 1: con soft delete, un género dado de baja sigue en
        // la tabla, así que sin el filtro el índice chocaría contra sí mismo apenas
        // se reuse ese nombre.
        builder.HasIndex(e => e.Name).IsUnique().HasFilter("[IsActive] = 1");

        builder.HasQueryFilter(e => e.IsActive);
    }
}
