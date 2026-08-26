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

    // Solo un valor de conveniencia para precargar el precio de una función nueva; el
    // precio real de venta sigue siendo Showtime.Price, editable función por función.
    public decimal? SuggestedPrice { get; set; }

    public bool IsActive { get; set; } = true;
}