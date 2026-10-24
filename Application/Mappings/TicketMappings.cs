using Application.DTOs.Tickets;
using Domain.Entities;

namespace Application.Mappings;

public static class TicketMappings
{
    public static TicketResponse ToResponse(this Ticket ticket) => new(
        ticket.Id, ticket.MovieId, ticket.ScreenId, ticket.UserId, ticket.BuyDate, ticket.FinalPrice, ticket.IsActive);

    // GetValueOrDefault: [Required] ya garantizó los valores antes de llegar al servicio.
    public static Ticket ToEntity(this CreateTicketRequest request) => new()
    {
        MovieId = request.MovieId.GetValueOrDefault(),
        ScreenId = request.ScreenId.GetValueOrDefault(),
        UserId = request.UserId.GetValueOrDefault(),
        BuyDate = request.BuyDate.GetValueOrDefault(),
        FinalPrice = request.FinalPrice,
    };

    public static void ApplyTo(this UpdateTicketRequest request, Ticket ticket)
    {
        ticket.MovieId = request.MovieId.GetValueOrDefault();
        ticket.ScreenId = request.ScreenId.GetValueOrDefault();
        ticket.UserId = request.UserId.GetValueOrDefault();
        ticket.BuyDate = request.BuyDate.GetValueOrDefault();
        ticket.FinalPrice = request.FinalPrice;
    }
}
