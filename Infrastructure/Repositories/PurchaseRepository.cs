using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class PurchaseRepository(CinemAPIContext context) : BaseRepository<Purchase>(context), IPurchaseRepository
{
    private static readonly Expression<Func<PurchaseRow, PurchaseDetails>> ToDetails = r => new PurchaseDetails(
        r.Purchase.Id,
        r.Purchase.UserId,
        r.User.Name,
        r.User.Email,
        r.Showtime.Id,
        r.Showtime.StartTime,
        r.Movie.Id,
        r.Movie.Title,
        r.Screen.Id,
        r.Screen.Name,
        r.Purchase.CreatedAt,
        r.Purchase.Subtotal,
        r.Purchase.FeeAmount,
        r.Purchase.Total,
        r.Purchase.Status,
        r.Purchase.CancelledAt,
        r.Purchase.Tickets
            .Where(t => t.IsActive)
            .OrderBy(t => t.SeatRow).ThenBy(t => t.SeatNumber)
            .Select(t => new PurchasedSeat(t.Id, t.SeatRow, t.SeatNumber, t.Price))
            .ToList());

    // Con las entradas: cancelar la compra también las cancela.
    public override Task<Purchase?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Context.Purchases.Include(p => p.Tickets).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PurchaseDetails?> GetDetailsAsync(Guid id, CancellationToken ct) =>
        Rows().Where(r => r.Purchase.Id == id).Select(ToDetails).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<PurchaseDetails>> GetByUserAsync(Guid userId, CancellationToken ct) =>
        await Rows()
            .Where(r => r.Purchase.UserId == userId)
            .OrderByDescending(r => r.Showtime.StartTime)
            .Select(ToDetails)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PurchaseDetails>> SearchAsync(PurchaseFilter filter, CancellationToken ct)
    {
        var rows = Rows();
        if (filter.MovieId is { } movieId)
        {
            rows = rows.Where(r => r.Movie.Id == movieId);
        }

        if (filter.ShowtimeId is { } showtimeId)
        {
            rows = rows.Where(r => r.Showtime.Id == showtimeId);
        }

        if (filter.From is { } from)
        {
            rows = rows.Where(r => r.Purchase.CreatedAt >= from);
        }

        if (filter.To is { } to)
        {
            rows = rows.Where(r => r.Purchase.CreatedAt < to);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            rows = rows.Where(r => r.Purchase.Status == filter.Status);
        }

        return await rows.OrderByDescending(r => r.Purchase.CreatedAt).Select(ToDetails).ToListAsync(ct);
    }

    // IgnoreQueryFilters vale para toda la consulta: la compra y sus entradas se filtran a mano, y la
    // película, la sala, la función y el cliente se muestran aunque después se hayan dado de baja.
    private IQueryable<PurchaseRow> Rows() =>
        from p in Context.Purchases.IgnoreQueryFilters().AsNoTracking()
        where p.IsActive
        join s in Context.Showtimes on p.ShowtimeId equals s.Id
        join m in Context.Movies on s.MovieId equals m.Id
        join sc in Context.Screens on s.ScreenId equals sc.Id
        join u in Context.Users on p.UserId equals u.Id
        select new PurchaseRow { Purchase = p, Showtime = s, Movie = m, Screen = sc, User = u };

    private sealed class PurchaseRow
    {
        public Purchase Purchase { get; init; } = null!;
        public Showtime Showtime { get; init; } = null!;
        public Movie Movie { get; init; } = null!;
        public Screen Screen { get; init; } = null!;
        public User User { get; init; } = null!;
    }
}
