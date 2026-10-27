using Application.DTOs.Showtimes;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// Orquesta IShowtimeRepository + IMovieRepository (necesito la duración de la
// película para calcular solapamientos) + IScreenRepository (la sala tiene que existir)
// + ITicketRepository (butacas vendidas: el mapa y lo que no se puede tocar de una función con ventas).
public sealed class ShowtimeService(
    IShowtimeRepository showtimeRepository,
    IMovieRepository movieRepository,
    IScreenRepository screenRepository,
    ITicketRepository ticketRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IShowtimeService
{
    private const string EntityName = "la función";

    public async Task<IReadOnlyList<ShowtimeResponse>> GetAllAsync(CancellationToken ct) =>
        ToResponses(await showtimeRepository.GetAllAsync(ct));

    public async Task<ShowtimeResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<IReadOnlyList<ShowtimeResponse>> GetByMovieIdAsync(Guid movieId, CancellationToken ct) =>
        ToResponses(await showtimeRepository.GetByMovieIdAsync(movieId, ct));

    // Las que todavía no empezaron, de una película o de todas. No mira si la película está en cartelera:
    // eso lo informa el mapa de butacas (IsOnSale) y lo valida la compra.
    public async Task<IReadOnlyList<ShowtimeResponse>> GetUpcomingAsync(Guid? movieId, CancellationToken ct) =>
        ToResponses(await showtimeRepository.GetUpcomingAsync(movieId, timeProvider.Now(), ct));

    // Cartelera de una sala en un día (la usa el calendario del front).
    public async Task<IReadOnlyList<ShowtimeResponse>> GetByScreenAndDateAsync(
        Guid screenId, DateOnly date, CancellationToken ct) =>
        ToResponses(await showtimeRepository.GetByScreenAndDateAsync(screenId, date.ToDateTime(TimeOnly.MinValue), ct));

    public async Task<SeatMapResponse> GetSeatMapAsync(Guid id, CancellationToken ct)
    {
        var showtime = await GetEntityAsync(id, ct);
        var screen = await screenRepository.GetByIdAsync(showtime.ScreenId, ct)
            ?? throw new BusinessRuleException("La sala de esta función no está disponible.");
        var movie = await movieRepository.GetByIdAsync(showtime.MovieId, ct);
        var taken = await ticketRepository.GetTakenSeatsAsync(id, ct);
        var isOnSale = movie is { IsShowing: true } && showtime.StartTime > timeProvider.Now();

        return new SeatMapResponse(
            showtime.Id,
            showtime.MovieId,
            movie?.Title ?? "Película no disponible",
            screen.Id,
            screen.Name,
            showtime.StartTime,
            showtime.Price,
            screen.Rows,
            screen.SeatsPerRow,
            isOnSale,
            taken.Select(s => new SeatResponse(s.Row, s.Number)).ToList());
    }

    // Id lo generamos acá, no lo mandamos a confiar del cliente.
    public async Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken ct)
    {
        var showtime = request.ToEntity();
        showtime.Id = Guid.NewGuid();
        showtime.IsActive = true;
        await ValidateNoOverlapAsync(showtime, previous: null, ct);

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
        // El horario sí se puede correr (reprogramación); la sala y la película no, porque las entradas ya
        // vendidas dejarían de corresponder a lo que se compró.
        var changesWhatWasSold = updated.ScreenId != existing.ScreenId || updated.MovieId != existing.MovieId;
        if (changesWhatWasSold && await ticketRepository.HasSoldTicketsAsync(id, ct))
        {
            throw new ShowtimeConflictException("La función tiene entradas vendidas: no se puede cambiar la sala ni la película.");
        }

        await ValidateNoOverlapAsync(updated, previous: existing, ct);

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
        if (await ticketRepository.HasSoldTicketsAsync(id, ct))
        {
            throw new ShowtimeConflictException("La función tiene entradas vendidas: cancelá esas compras antes de borrarla.");
        }

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
    // En una edición solo se validan las referencias que cambian (igual que en Movie y Ticket): una
    // función de una película ya dada de baja se puede seguir reprogramando.
    private async Task ValidateNoOverlapAsync(Showtime showtime, Showtime? previous, CancellationToken ct)
    {
        int durationMinutes;
        if (showtime.MovieId != previous?.MovieId)
        {
            var movie = await movieRepository.GetByIdAsync(showtime.MovieId, ct)
                ?? throw new InvalidReferenceException("la película", showtime.MovieId);
            durationMinutes = movie.DurationMinutes;
        }
        else
        {
            durationMinutes = await movieRepository.GetDurationIncludingDeletedAsync(showtime.MovieId, ct)
                ?? throw new InvalidReferenceException("la película", showtime.MovieId);
        }

        if (showtime.ScreenId != previous?.ScreenId && !await screenRepository.ExistsAsync(showtime.ScreenId, ct))
        {
            throw new InvalidReferenceException("la sala", showtime.ScreenId);
        }

        var newStart = showtime.StartTime;
        var newEnd = newStart.AddMinutes(durationMinutes + Showtime.CleaningBufferMinutes);

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
