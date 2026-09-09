using System;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

// Orquesta IShowtimeRepository + IMovieRepository (necesito la duración de la
// película para calcular solapamientos). La llama ShowtimeController.
public class ShowtimeService
{
    // Minutos de limpieza que se suman a la duración de la película para calcular el
    // tramo real que ocupa una función en la sala.
    private const int CLEANING_BUFFER_MINUTES = 15;

    private readonly IShowtimeRepository _repo;
    private readonly IMovieRepository _movieRepo;

    public ShowtimeService(IShowtimeRepository showtimeRepository, IMovieRepository movieRepository)
    {
        _repo = showtimeRepository;
        _movieRepo = movieRepository;
    }

    public async Task<IEnumerable<Showtime>> GetAll()
    {
        return await _repo.GetAll();
    }

    public async Task<Showtime?> GetById(Guid id)
    {
        return await _repo.GetById(id);
    }

    public async Task<IEnumerable<Showtime>> GetByMovieId(Guid movieId)
    {
        return await _repo.GetByMovieId(movieId);
    }

    // Reusado por ValidateNoOverlap para traer las candidatas a chocar.
    public async Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date)
    {
        return await _repo.GetByScreenAndDate(screenId, date);
    }

    // Id lo generamos acá, no lo mandamos a confiar del cliente.
    public async Task<Showtime> Add(Showtime showtime)
    {
        showtime.Id = Guid.NewGuid();
        showtime.IsActive = true;
        await ValidateNoOverlap(showtime);
        return await _repo.Add(showtime);
    }

    // Re-valida solapamiento con los nuevos datos (ValidateNoOverlap excluye el propio
    // Id, así que no choca contra sí mismo).
    public async Task Update(Showtime showtime)
    {
        await ValidateNoOverlap(showtime);
        await _repo.Update(showtime);
    }

    // Soft delete: apaga IsActive en vez de borrar la fila.
    public async Task Delete(Guid id)
    {
        var showtime = await _repo.GetById(id);
        if (showtime != null)
        {
            showtime.IsActive = false;
            await _repo.Update(showtime);
        }
    }

    // El tramo que ocupa una función es [StartTime, StartTime + duración de la película
    // + CLEANING_BUFFER_MINUTES]; dos tramos chocan si se cruzan aunque sea
    // parcialmente (tocarse justo en el borde no cuenta como choque). Solo compara
    // funciones del mismo día calendario que StartTime: no hay funciones que crucen
    // la medianoche.
    private async Task ValidateNoOverlap(Showtime showtime)
    {
        var movie = await _movieRepo.GetById(showtime.MovieId);
        if (movie == null)
        {
            throw new ShowtimeConflictException($"No se encontró la película con Id {showtime.MovieId}.");
        }

        var newStart = showtime.StartTime;
        var newEnd = newStart.AddMinutes(movie.DurationMinutes + CLEANING_BUFFER_MINUTES);

        // Traigo todas las funciones activas de esa sala en ese día y comparo en memoria.
        var candidates = await _repo.GetByScreenAndDate(showtime.ScreenId, newStart.Date);

        // Cache para no pedir la misma película dos veces si hay varias candidatas.
        var movieCache = new Dictionary<Guid, Movie?> { [movie.Id] = movie };

        foreach (var other in candidates)
        {
            if (other.Id == showtime.Id) continue; // excluye el propio registro (caso Update)

            if (!movieCache.TryGetValue(other.MovieId, out var otherMovie))
            {
                otherMovie = await _movieRepo.GetById(other.MovieId);
                movieCache[other.MovieId] = otherMovie;
            }

            var otherEnd = other.StartTime.AddMinutes((otherMovie?.DurationMinutes ?? 0) + CLEANING_BUFFER_MINUTES);

            // Choque clásico de intervalos: se pisan si cada uno empieza antes de que
            // termine el otro. Con < (no <=) tocarse justo en el borde no cuenta.
            var overlaps = newStart < otherEnd && other.StartTime < newEnd;
            if (overlaps)
            {
                throw new ShowtimeConflictException(
                    $"La sala ya tiene programada la función '{otherMovie?.Title ?? "otra película"}' " +
                    $"(Showtime Id {other.Id}) de {other.StartTime:HH:mm} a {otherEnd:HH:mm} el " +
                    $"{other.StartTime:dd/MM/yyyy}; esta función se superpone con esa franja.");
            }
        }
    }
}
