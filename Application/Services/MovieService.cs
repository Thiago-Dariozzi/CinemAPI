using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MovieService(IMovieRepository movieRepository, IUnitOfWork unitOfWork) : IMovieService
{
    private const string EntityName = "la película";

    public Task<IReadOnlyList<Movie>> GetActiveMoviesAsync(CancellationToken ct) => movieRepository.GetActiveMoviesAsync(ct);

    public Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct) =>
        movieRepository.GetByGenreAsync(genreId, ct);

    public Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct) =>
        movieRepository.GetByTitleAsync(title, ct);

    public Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken ct) => movieRepository.GetAllAsync(ct);

    public async Task<Movie> GetByIdAsync(Guid id, CancellationToken ct) =>
        await movieRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    public async Task<Movie> AddAsync(Movie movie, CancellationToken ct)
    {
        movie.Id = Guid.NewGuid();
        movie.IsActive = true;
        movieRepository.Add(movie);
        await unitOfWork.SaveChangesAsync(ct);
        return movie;
    }

    public async Task UpdateAsync(Movie movie, CancellationToken ct)
    {
        var existing = await GetByIdAsync(movie.Id, ct);

        existing.Title = movie.Title;
        existing.Synopsis = movie.Synopsis;
        existing.DurationMinutes = movie.DurationMinutes;
        existing.GenreId = movie.GenreId;
        existing.ImageUrl = movie.ImageUrl;
        existing.ReleaseDate = movie.ReleaseDate;
        existing.SuggestedPrice = movie.SuggestedPrice;
        existing.IsActive = movie.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var movie = await GetByIdAsync(id, ct);
        movie.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
    }
}
