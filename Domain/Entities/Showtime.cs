using System;

namespace Domain.Entities;

public class Showtime
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Guid ScreenId { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsActive { get; set; } = true;
}
