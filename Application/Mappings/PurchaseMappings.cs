using Application.DTOs.Purchases;
using Domain.Models;

namespace Application.Mappings;

public static class PurchaseMappings
{
    public static PurchaseResponse ToResponse(this PurchaseDetails purchase, bool canCancel) => new(
        purchase.Id,
        purchase.UserId,
        purchase.UserName,
        purchase.UserEmail,
        purchase.ShowtimeId,
        purchase.StartTime,
        purchase.MovieId,
        purchase.MovieTitle,
        purchase.ScreenId,
        purchase.ScreenName,
        purchase.CreatedAt,
        purchase.Subtotal,
        purchase.FeeAmount,
        purchase.Total,
        purchase.Status,
        purchase.CancelledAt,
        canCancel,
        purchase.Seats.Select(s => new PurchaseTicketResponse(s.TicketId, s.Row, s.Number, s.Price)).ToList());
}
