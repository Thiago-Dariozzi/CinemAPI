using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MovieService(IMovieRepository movieRepository, IUnitOfWork unitOfWork)
{
    public async Task<IEnumerable<Movie>> GetActiveMovies()
    {
        return await movieRepository.GetActiveMovies();
    }

    public async Task<IEnumerable<Movie>> GetByGenre(Guid genreId)
    {
        return await movieRepository.GetByGenre(genreId);
    }

    public async Task<IEnumerable<Movie>> GetByTitle(string title)
    {
        return await movieRepository.GetByTitle(title);
    }

    public async Task<IEnumerable<Movie>> GetAll()
    {
        return await movieRepository.GetAll();
    }

    public async Task<Movie?> GetById(Guid id)
    {
        return await movieRepository.GetById(id);
    }

    public async Task<Movie> Add(Movie movie)
    {
        movie.Id = Guid.NewGuid();
        movie.IsActive = true;
        movieRepository.Add(movie);
        await unitOfWork.SaveChangesAsync();
        return movie;
    }

    public async Task Delete(Guid id)
    {
        var movie = await movieRepository.GetById(id);
        if (movie != null)
        {
            movie.IsActive = false;
            await unitOfWork.SaveChangesAsync();
        }
    }

    public async Task Update(Movie movie)
    {
        var existing = await movieRepository.GetById(movie.Id);
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
        await unitOfWork.SaveChangesAsync();
    }
}
