using Application.DTOs.Showtimes;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// Orquesta IShowtimeRepository + IMovieRepository (necesito la duración de la
// película para calcular solapamientos) + IScreenRepository (la sala tiene que existir).
public sealed class ShowtimeService(
    IShowtimeRepository showtimeRepository,
    IMovieRepository movieRepository,
    IScreenRepository screenRepository,
    IUnitOfWork unitOfWork) : IShowtimeService
{
    private const string EntityName = "la función";

    public async Task<IReadOnlyList<ShowtimeResponse>> GetAllAsync(CancellationToken ct) =>
        ToResponses(await showtimeRepository.GetAllAsync(ct));

    public async Task<ShowtimeResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<IReadOnlyList<ShowtimeResponse>> GetByMovieIdAsync(Guid movieId, CancellationToken ct) =>
        ToResponses(await showtimeRepository.GetByMovieIdAsync(movieId, ct));

    // Cartelera de una sala en un día (la usa el calendario del front).
    public async Task<IReadOnlyList<ShowtimeResponse>> GetByScreenAndDateAsync(
        Guid screenId, DateOnly date, CancellationToken ct) =>
        ToResponses(await showtimeRepository.GetByScreenAndDateAsync(screenId, date.ToDateTime(TimeOnly.MinValue), ct));

    // Id lo generamos acá, no lo mandamos a confiar del cliente.
    public async Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken ct)
    {
        var showtime = request.ToEntity();
        showtime.Id = Guid.NewGuid();
        showtime.IsActive = true;
        await ValidateNoOverlapAsync(showtime, ct);

        showtimeRepository.Add(showtime);
        await unitOfWork.SaveChangesAsync(ct);
        return showtime.ToResponse();
    }

    // Valida con los datos nuevos antes de tocar la entidad trackeada (ValidateNoOverlapAsync
    // excluye el propio Id, así que no choca contra sí misma).
    public async Task UpdateAsync(Guid id, UpdateShowtimeRequest request, CancellationToken ct)
    {
        var existing = await GetEntityAsync(id, ct);
        var updated = request.ToEntity(id);

        await ValidateNoOverlapAsync(updated, ct);

        existing.MovieId = updated.MovieId;
        existing.ScreenId = updated.ScreenId;
        existing.StartTime = updated.StartTime;
        existing.Price = updated.Price;
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Soft delete: apaga IsActive en vez de borrar la fila.
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var showtime = await GetEntityAsync(id, ct);
        showtime.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Showtime> GetEntityAsync(Guid id, CancellationToken ct) =>
        await showtimeRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    private static List<ShowtimeResponse> ToResponses(IEnumerable<Showtime> showtimes) =>
        showtimes.Select(s => s.ToResponse()).ToList();

    // El tramo que ocupa una función es [StartTime, StartTime + duración de la película
    // + CleaningBufferMinutes]; dos tramos chocan si se cruzan aunque sea
    // parcialmente (tocarse justo en el borde no cuenta como choque).
    private async Task ValidateNoOverlapAsync(Showtime showtime, CancellationToken ct)
    {
        var movie = await movieRepository.GetByIdAsync(showtime.MovieId, ct)
            ?? throw new InvalidReferenceException("la película", showtime.MovieId);
        if (!await screenRepository.ExistsAsync(showtime.ScreenId, ct))
        {
            throw new InvalidReferenceException("la sala", showtime.ScreenId);
        }

        var newStart = showtime.StartTime;
        var newEnd = newStart.AddMinutes(movie.DurationMinutes + Showtime.CleaningBufferMinutes);

        // Candidatas: las que empiezan desde 24 h antes (ninguna película dura un día, así que una
        // función de la noche anterior que cruce la medianoche entra) hasta el fin de la nueva.
        var occupied = await showtimeRepository.GetOccupancyByScreenAsync(
            showtime.ScreenId, newStart.AddDays(-1), newEnd, ct);

        foreach (var other in occupied.Where(o => o.ShowtimeId != showtime.Id))
        {
            var otherEnd = other.StartTime.AddMinutes(other.DurationMinutes + Showtime.CleaningBufferMinutes);

            // Choque clásico de intervalos: se pisan si cada uno empieza antes de que
            // termine el otro. Con < (no <=) tocarse justo en el borde no cuenta.
            var overlaps = newStart < otherEnd && other.StartTime < newEnd;
            if (overlaps)
            {
                throw new ShowtimeConflictException(
                    $"La sala ya tiene programada la función '{other.MovieTitle}' " +
                    $"(Showtime Id {other.ShowtimeId}) de {other.StartTime:HH:mm} a {otherEnd:HH:mm} el " +
                    $"{other.StartTime:dd/MM/yyyy}; esta función se superpone con esa franja.");
            }
        }
    }
}
