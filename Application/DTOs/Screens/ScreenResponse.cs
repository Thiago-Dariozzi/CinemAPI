namespace Application.DTOs.Screens;

public sealed record ScreenResponse(Guid Id, string Name, int Capacity, bool IsActive);
