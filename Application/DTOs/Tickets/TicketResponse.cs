namespace Application.DTOs.Tickets;

public sealed record TicketResponse(
    Guid Id, Guid MovieId, Guid ScreenId, Guid UserId, DateTime BuyDate, decimal FinalPrice, bool IsActive);
