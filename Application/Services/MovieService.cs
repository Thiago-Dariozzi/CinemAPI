using Application.DTOs.Movies;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MovieService(
    IMovieRepository movieRepository,
    IGenreRepository genreRepository,
    IShowtimeRepository showtimeRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork) : IMovieService
{
    private const string EntityName = "la película";

    // Con fecha: solo las que tienen una función ese día que todavía no empezó (hoy no cuentan las de la
    // mañana que ya pasaron). Un día pasado no tiene ninguna.
    public async Task<IReadOnlyList<MovieResponse>> GetShowingMoviesAsync(Guid? genreId, DateOnly? date, CancellationToken ct)
    {
        if (date is { } requested)
        {
            DateRules.EnsureHasNextDay(requested);
        }

        var movies = await movieRepository.GetShowingMoviesAsync(genreId, ct);
        if (date is not { } day)
        {
            return ToResponses(movies);
        }

        var dayStart = day.ToDateTime(TimeOnly.MinValue);
        var now = timeProvider.Now();
        // Igual que GetUpcomingAsync y la compra: la que empieza justo ahora ya no cuenta.
        var from = now >= dayStart ? now.AddTicks(1) : dayStart;
        var to = dayStart.AddDays(1);
        if (from >= to)
        {
            return [];
        }

        var withShowtimes = (await showtimeRepository.GetMovieIdsWithShowtimesAsync(from, to, ct)).ToHashSet();
        return ToResponses(movies.Where(m => withShowtimes.Contains(m.Id)));
    }

    public async Task<IReadOnlyList<MovieResponse>> GetByGenreAsync(Guid genreId, CancellationToken ct) =>
        ToResponses(await movieRepository.GetByGenreAsync(genreId, ct));

    public async Task<IReadOnlyList<MovieResponse>> GetByTitleAsync(string title, CancellationToken ct) =>
        ToResponses(await movieRepository.GetByTitleAsync(title, ct));

    public async Task<IReadOnlyList<MovieResponse>> GetAllAsync(CancellationToken ct) =>
        ToResponses(await movieRepository.GetAllAsync(ct));

    public async Task<MovieResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<MovieResponse> CreateAsync(CreateMovieRequest request, CancellationToken ct)
    {
        var movie = request.ToEntity();
        await EnsureGenreExistsAsync(movie.GenreId, ct);

        movie.Id = Guid.NewGuid();
        movie.IsActive = true;
        movieRepository.Add(movie);
        await unitOfWork.SaveChangesAsync(ct);
        return movie.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateMovieRequest request, CancellationToken ct)
    {
        var movie = await GetEntityAsync(id, ct);

        // Solo si cambia: una película cuyo género se dio de baja se puede seguir editando.
        if (request.GenreId != movie.GenreId)
        {
            await EnsureGenreExistsAsync(request.GenreId.GetValueOrDefault(), ct);
        }

        request.ApplyTo(movie);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var movie = await GetEntityAsync(id, ct);
        movie.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<Movie> GetEntityAsync(Guid id, CancellationToken ct) =>
        await movieRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    private async Task EnsureGenreExistsAsync(Guid genreId, CancellationToken ct)
    {
        if (!await genreRepository.ExistsAsync(genreId, ct))
        {
            throw new InvalidReferenceException("el género", genreId);
        }
    }

    private static List<MovieResponse> ToResponses(IEnumerable<Movie> movies) =>
        movies.Select(m => m.ToResponse()).ToList();
}
