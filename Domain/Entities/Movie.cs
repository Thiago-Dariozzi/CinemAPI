using System;

namespace Domain.Entities;

public class Movie
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Synopsis { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public Guid GenreId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }

    // Precio "sugerido" de catálogo: es solo un valor de conveniencia para precargar
    // el precio al armar una función nueva en el frontend. No tiene ninguna lógica de
    // negocio ni validación asociada; el precio real de venta sigue siendo Showtime.Price,
    // que siempre se puede editar función por función.
    public decimal? SuggestedPrice { get; set; }

    public bool IsActive { get; set; } = true;
}