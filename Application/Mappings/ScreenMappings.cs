using Application.DTOs.Screens;
using Domain.Entities;

namespace Application.Mappings;

public static class ScreenMappings
{
    public static ScreenResponse ToResponse(this Screen screen) =>
        new(screen.Id, screen.Name, screen.Rows, screen.SeatsPerRow, screen.Capacity, screen.IsActive);

    public static Screen ToEntity(this CreateScreenRequest request) =>
        new() { Name = request.Name, Rows = request.Rows, SeatsPerRow = request.SeatsPerRow };

    public static void ApplyTo(this UpdateScreenRequest request, Screen screen)
    {
        screen.Name = request.Name;
        screen.Rows = request.Rows;
        screen.SeatsPerRow = request.SeatsPerRow;
    }
}
