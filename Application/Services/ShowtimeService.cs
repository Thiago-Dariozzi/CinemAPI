using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// Orquesta IShowtimeRepository + IMovieRepository (necesito la duración de la
// película para calcular solapamientos). La llama ShowtimeController.
public sealed class ShowtimeService(
    IShowtimeRepository showtimeRepository,
    IMovieRepository movieRepository,
    IUnitOfWork unitOfWork) : IShowtimeService
{
    private const string EntityName = "la función";

    public Task<IReadOnlyList<Showtime>> GetAllAsync(CancellationToken ct) => showtimeRepository.GetAllAsync(ct);

    public async Task<Showtime> GetByIdAsync(Guid id, CancellationToken ct) =>
        await showtimeRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    public Task<IReadOnlyList<Showtime>> GetByMovieIdAsync(Guid movieId, CancellationToken ct) =>
        showtimeRepository.GetByMovieIdAsync(movieId, ct);

    // Cartelera de una sala en un día (la usa el calendario del front).
    public Task<IReadOnlyList<Showtime>> GetByScreenAndDateAsync(Guid screenId, DateTime date, CancellationToken ct) =>
        showtimeRepository.GetByScreenAndDateAsync(screenId, date, ct);

    // Id lo generamos acá, no lo mandamos a confiar del cliente.
    public async Task<Showtime> AddAsync(Showtime showtime, CancellationToken ct)
    {
        showtime.Id = Guid.NewGuid();
        showtime.IsActive = true;
        await ValidateNoOverlapAsync(showtime, ct);

        showtimeRepository.Add(showtime);
        await unitOfWork.SaveChangesAsync(ct);
        return showtime;
    }

    // Valida con los datos nuevos antes de tocar la entidad trackeada (ValidateNoOverlapAsync
    // excluye el propio Id, así que no choca contra sí misma).
    public async Task UpdateAsync(Showtime showtime, CancellationToken ct)
    {
        var existing = await GetByIdAsync(showtime.Id, ct);

        await ValidateNoOverlapAsync(showtime, ct);

        existing.MovieId = showtime.MovieId;
        existing.ScreenId = showtime.ScreenId;
        existing.StartTime = showtime.StartTime;
        existing.Price = showtime.Price;
        existing.IsActive = showtime.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Soft delete: apaga IsActive en vez de borrar la fila.
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var showtime = await GetByIdAsync(id, ct);
        showtime.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    // El tramo que ocupa una función es [StartTime, StartTime + duración de la película
    // + CleaningBufferMinutes]; dos tramos chocan si se cruzan aunque sea
    // parcialmente (tocarse justo en el borde no cuenta como choque). Solo compara
    // funciones del mismo día calendario que StartTime.
    private async Task ValidateNoOverlapAsync(Showtime showtime, CancellationToken ct)
    {
        var movie = await movieRepository.GetByIdAsync(showtime.MovieId, ct)
            ?? throw new ShowtimeConflictException($"No se encontró la película con Id {showtime.MovieId}.");

        var newStart = showtime.StartTime;
        var newEnd = newStart.AddMinutes(movie.DurationMinutes + Showtime.CleaningBufferMinutes);

        // Una sola consulta trae las funciones de esa sala en ese día con la duración de su película.
        var occupied = await showtimeRepository.GetOccupancyByScreenAndDateAsync(showtime.ScreenId, newStart, ct);

        foreach (var other in occupied.Where(o => o.ShowtimeId != showtime.Id))
        {
            var otherEnd = other.StartTime.AddMinutes((other.DurationMinutes ?? 0) + Showtime.CleaningBufferMinutes);

            // Choque clásico de intervalos: se pisan si cada uno empieza antes de que
            // termine el otro. Con < (no <=) tocarse justo en el borde no cuenta.
            var overlaps = newStart < otherEnd && other.StartTime < newEnd;
            if (overlaps)
            {
                throw new ShowtimeConflictException(
                    $"La sala ya tiene programada la función '{other.MovieTitle ?? "otra película"}' " +
                    $"(Showtime Id {other.ShowtimeId}) de {other.StartTime:HH:mm} a {otherEnd:HH:mm} el " +
                    $"{other.StartTime:dd/MM/yyyy}; esta función se superpone con esa franja.");
            }
        }
    }
}
