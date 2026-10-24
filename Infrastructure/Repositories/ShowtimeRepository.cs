using Domain.Entities;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class ShowtimeRepository(CinemAPIContext context) : BaseRepository<Showtime>(context), IShowtimeRepository
{
    public override async Task<IReadOnlyList<Showtime>> GetAllAsync(CancellationToken ct) =>
        await Context.Showtimes.AsNoTracking()
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct) =>
        await Context.Showtimes.AsNoTracking()
            .Where(s => s.MovieId == movieId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct) =>
        await ByScreenAndDay(screenId, date)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    // Left join: si la película fue dada de baja, el filtro global la oculta y la función
    // vuelve con duración null (el servicio decide qué hacer en ese caso).
    public async Task<IReadOnlyList<ShowtimeOccupancy>> GetOccupancyByScreenAndDateAsync(
        Guid screenId, DateTime date, CancellationToken ct)
    {
        var rows = await ByScreenAndDay(screenId, date)
            .OrderBy(s => s.StartTime)
            .LeftJoin(Context.Movies, s => s.MovieId, m => m.Id, (s, m) => new
            {
                s.Id,
                s.StartTime,
                DurationMinutes = m == null ? (int?)null : m.DurationMinutes,
                Title = m == null ? null : m.Title,
            })
            .ToListAsync(ct);

        return rows.Select(r => new ShowtimeOccupancy(r.Id, r.StartTime, r.DurationMinutes, r.Title)).ToList();
    }

    // Rango [día, día + 1) en vez de StartTime.Date == date: así puede usar el índice (ScreenId, StartTime).
    private IQueryable<Showtime> ByScreenAndDay(Guid screenId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        return Context.Showtimes.AsNoTracking()
            .Where(s => s.ScreenId == screenId && s.StartTime >= dayStart && s.StartTime < dayEnd);
    }
}
