using Domain.Models;

namespace Application.Services;

// Cómo se nombran las butacas en los mensajes: fila en letra (1 = "A") + número, igual que en el front.
internal static class SeatLabels
{
    public static string Of(int row, int number) => $"{(char)('A' + row - 1)}{number}";

    public static string Of(Seat seat) => Of(seat.Row, seat.Number);

    public static string Join(IEnumerable<Seat> seats) => string.Join(", ", seats.Select(Of));
}
