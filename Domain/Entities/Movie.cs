using System;

namespace Domain.Entities;

public class Movie
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Synopsis { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public bool IsActive { get; set; } = true;
}