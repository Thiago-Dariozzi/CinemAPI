using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class TicketRepository(CinemAPIContext context) : BaseRepository<Ticket>(context), ITicketRepository
{
    public async Task<IReadOnlyList<Seat>> GetTakenSeatsAsync(Guid showtimeId, CancellationToken ct) =>
        await Sold()
            .Where(t => t.ShowtimeId == showtimeId)
            .OrderBy(t => t.SeatRow).ThenBy(t => t.SeatNumber)
            .Select(t => new Seat(t.SeatRow, t.SeatNumber))
            .ToListAsync(ct);

    public Task<bool> HasSoldTicketsAsync(Guid showtimeId, CancellationToken ct) =>
        Sold().AnyAsync(t => t.ShowtimeId == showtimeId, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> CountSoldByShowtimeAsync(Guid movieId, DateTime after, CancellationToken ct) =>
        await Sold()
            .Join(Context.Showtimes, t => t.ShowtimeId, s => s.Id, (t, s) => new { t.ShowtimeId, s.MovieId, s.StartTime })
            .Where(x => x.MovieId == movieId && x.StartTime > after)
            .GroupBy(x => x.ShowtimeId)
            .Select(g => new { ShowtimeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShowtimeId, x => x.Count, ct);

    public Task<bool> HasSoldSeatsOutsideAsync(Guid screenId, int rows, int seatsPerRow, DateTime after, CancellationToken ct) =>
        Sold()
            .Join(Context.Showtimes, t => t.ShowtimeId, s => s.Id, (t, s) => new { t.SeatRow, t.SeatNumber, s.ScreenId, s.StartTime })
            .AnyAsync(x => x.ScreenId == screenId && x.StartTime > after && (x.SeatRow > rows || x.SeatNumber > seatsPerRow), ct);

    private IQueryable<Ticket> Sold() => Context.Tickets.AsNoTracking().Where(t => !t.IsCancelled);
}
