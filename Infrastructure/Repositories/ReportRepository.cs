using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

// IgnoreQueryFilters vale para toda la consulta: las compras, las entradas y las funciones se filtran a
// mano por IsActive, y la película y la sala cuentan aunque después se hayan dado de baja (lo vendido
// sigue siendo vendido).
public sealed class ReportRepository(CinemAPIContext context) : IReportRepository
{
    public async Task<IReadOnlyList<MovieSalesStat>> GetSalesByMovieAsync(DateTime start, DateTime end, CancellationToken ct)
    {
        // Dos agrupaciones: los importes son de la compra (el cargo no se reparte por entrada) y la
        // cantidad de entradas sale de la tabla de entradas.
        var amounts = await (
            from p in Confirmed(start, end)
            join s in context.Showtimes on p.ShowtimeId equals s.Id
            join m in context.Movies on s.MovieId equals m.Id
            group p by new { m.Id, m.Title } into g
            select new
            {
                MovieId = g.Key.Id,
                g.Key.Title,
                Purchases = g.Count(),
                Subtotal = g.Sum(p => p.Subtotal),
                FeeAmount = g.Sum(p => p.FeeAmount),
                Total = g.Sum(p => p.Total),
            }).ToListAsync(ct);

        var tickets = await (
            from t in context.Tickets.IgnoreQueryFilters().AsNoTracking()
            where t.IsActive && !t.IsCancelled
            join p in Confirmed(start, end) on t.PurchaseId equals p.Id
            join s in context.Showtimes on t.ShowtimeId equals s.Id
            group t by s.MovieId into g
            select new { MovieId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.MovieId, x => x.Count, ct);

        return amounts
            .Select(a => new MovieSalesStat(a.MovieId, a.Title, a.Purchases, tickets.GetValueOrDefault(a.MovieId), a.Subtotal, a.FeeAmount, a.Total))
            .ToList();
    }

    public Task<int> CountCancelledTicketsAsync(DateTime start, DateTime end, CancellationToken ct) =>
        (from t in context.Tickets.IgnoreQueryFilters().AsNoTracking()
         where t.IsActive && t.IsCancelled
         join p in context.Purchases on t.PurchaseId equals p.Id
         where p.IsActive && p.CreatedAt >= start && p.CreatedAt < end
         select t.Id).CountAsync(ct);

    public async Task<IReadOnlyList<ShowtimeSeatsStat>> GetShowtimeSeatsAsync(DateTime start, DateTime end, CancellationToken ct) =>
        await (
            from s in context.Showtimes.IgnoreQueryFilters().AsNoTracking()
            where s.IsActive && s.StartTime >= start && s.StartTime < end
            join m in context.Movies on s.MovieId equals m.Id
            join sc in context.Screens on s.ScreenId equals sc.Id
            orderby s.StartTime, sc.Name
            select new ShowtimeSeatsStat(
                s.Id,
                s.StartTime,
                m.Id,
                m.Title,
                sc.Id,
                sc.Name,
                sc.Rows * sc.SeatsPerRow,
                context.Tickets.Count(t => t.ShowtimeId == s.Id && t.IsActive && !t.IsCancelled))).ToListAsync(ct);

    private IQueryable<Purchase> Confirmed(DateTime start, DateTime end) =>
        context.Purchases.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.IsActive && p.Status == PurchaseStatus.Confirmed && p.CreatedAt >= start && p.CreatedAt < end);
}
