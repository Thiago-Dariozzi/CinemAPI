using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MovieService(IMovieRepository movieRepository, IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<Movie>> GetActiveMoviesAsync(CancellationToken ct)
    {
        return await movieRepository.GetActiveMoviesAsync(ct);
    }

    public async Task<IReadOnlyList<Movie>> GetByGenreAsync(Guid genreId, CancellationToken ct)
    {
        return await movieRepository.GetByGenreAsync(genreId, ct);
    }

    public async Task<IReadOnlyList<Movie>> GetByTitleAsync(string title, CancellationToken ct)
    {
        return await movieRepository.GetByTitleAsync(title, ct);
    }

    public async Task<IReadOnlyList<Movie>> GetAllAsync(CancellationToken ct)
    {
        return await movieRepository.GetAllAsync(ct);
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await movieRepository.GetByIdAsync(id, ct);
    }

    public async Task<Movie> AddAsync(Movie movie, CancellationToken ct)
    {
        movie.Id = Guid.NewGuid();
        movie.IsActive = true;
        movieRepository.Add(movie);
        await unitOfWork.SaveChangesAsync(ct);
        return movie;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var movie = await movieRepository.GetByIdAsync(id, ct);
        if (movie != null)
        {
            movie.IsActive = false;
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateAsync(Movie movie, CancellationToken ct)
    {
        var existing = await movieRepository.GetByIdAsync(movie.Id, ct);
        if (existing == null)
        {
            return;
        }

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
}
