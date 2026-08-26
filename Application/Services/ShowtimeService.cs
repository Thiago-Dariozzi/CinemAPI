using System;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public class ShowtimeService
{
    // Margen de limpieza que se suma a la duración de la película para calcular el
    // tramo que ocupa una función en la sala. Constante fácil de ajustar.
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

    public async Task<IEnumerable<Showtime>> GetByScreenAndDate(Guid screenId, DateTime date)
    {
        return await _repo.GetByScreenAndDate(screenId, date);
    }

    public async Task<Showtime> Add(Showtime showtime)
    {
        showtime.Id = Guid.NewGuid();
        showtime.IsActive = true;
        await ValidateNoOverlap(showtime);
        return await _repo.Add(showtime);
    }

    public async Task Update(Showtime showtime)
    {
        await ValidateNoOverlap(showtime);
        await _repo.Update(showtime);
    }

    public async Task Delete(Guid id)
    {
        var showtime = await _repo.GetById(id);
        if (showtime != null)
        {
            showtime.IsActive = false;
            await _repo.Update(showtime);
        }
    }

    // Valida que la función no se pise con otra función activa de la MISMA sala.
    // El tramo que ocupa una función es [StartTime, StartTime + duración de la película
    // + CLEANING_BUFFER_MINUTES], y dos tramos chocan si se cruzan aunque sea
    // parcialmente (tocarse en el borde, ej. una termina justo cuando otra arranca, no
    // cuenta como choque). Solo se comparan funciones del mismo día calendario que
    // StartTime: alcanza para este dominio, donde no hay funciones que crucen la
    // medianoche.
    private async Task ValidateNoOverlap(Showtime showtime)
    {
        var movie = await _movieRepo.GetById(showtime.MovieId);
        if (movie == null)
        {
            throw new ShowtimeConflictException($"No se encontró la película con Id {showtime.MovieId}.");
        }

        var newStart = showtime.StartTime;
        var newEnd = newStart.AddMinutes(movie.DurationMinutes + CLEANING_BUFFER_MINUTES);

        var candidates = await _repo.GetByScreenAndDate(showtime.ScreenId, newStart.Date);

        // Cache de películas ya resueltas, para no repetir la consulta si varias
        // funciones candidatas son de la misma película.
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
