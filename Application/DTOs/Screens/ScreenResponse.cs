namespace Application.DTOs.Screens;

// Capacity = Rows × SeatsPerRow (calculada).
public sealed record ScreenResponse(Guid Id, string Name, int Rows, int SeatsPerRow, int Capacity, bool IsActive);
