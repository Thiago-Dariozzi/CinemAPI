namespace Application.DTOs.Reports;

// OccupancyPercentage: vendidas / capacidad × 100, con un decimal.
public sealed record ShowtimeOccupancyResponse(
    Guid ShowtimeId,
    DateTime StartTime,
    Guid MovieId,
    string MovieTitle,
    Guid ScreenId,
    string ScreenName,
    int TotalSeats,
    int SoldSeats,
    decimal OccupancyPercentage);
