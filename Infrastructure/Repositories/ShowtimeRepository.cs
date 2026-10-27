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

    public async Task<IReadOnlyList<Showtime>> GetUpcomingAsync(Guid? movieId, DateTime after, CancellationToken ct) =>
        await Context.Showtimes.AsNoTracking()
            .Where(s => s.StartTime > after && (movieId == null || s.MovieId == movieId))
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    // Rango [día, día + 1) en vez de StartTime.Date == date: así puede usar el índice (ScreenId, StartTime).
    public async Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);
        return await Context.Showtimes.AsNoTracking()
            .Where(s => s.ScreenId == screenId && s.StartTime >= dayStart && s.StartTime < dayEnd)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }

    // IgnoreQueryFilters en películas a propósito: si la película se dio de baja pero la función
    // sigue activa, la sala sigue ocupada durante toda su duración. La FK garantiza que existe.
    // IgnoreQueryFilters apaga los filtros de TODA la consulta, así que s.IsActive va explícito: sin él,
    // una función borrada seguía bloqueando la sala.
    public async Task<IReadOnlyList<ShowtimeOccupancy>> GetOccupancyByScreenAsync(
        Guid screenId, DateTime from, DateTime to, CancellationToken ct)
    {
        var rows = await Context.Showtimes.AsNoTracking()
            .Where(s => s.IsActive && s.ScreenId == screenId && s.StartTime >= from && s.StartTime < to)
            .OrderBy(s => s.StartTime)
            .Join(Context.Movies.IgnoreQueryFilters(), s => s.MovieId, m => m.Id, (s, m) => new
            {
                s.Id,
                s.StartTime,
                m.DurationMinutes,
                m.Title,
            })
            .ToListAsync(ct);

        return rows.Select(r => new ShowtimeOccupancy(r.Id, r.StartTime, r.DurationMinutes, r.Title)).ToList();
    }
}
