namespace Application.DTOs.Showtimes;

public sealed record ShowtimeResponse(Guid Id, Guid MovieId, Guid ScreenId, DateTime StartTime, decimal Price, bool IsActive);
