using System;
using Domain.Interfaces;
using Domain.Entities;
using Application.Services;


namespace Application.Services;

public class MovieService
{
    private readonly IMovieRepository _repo;
    public MovieService(IMovieRepository movieRepository)
    {
        _repo = movieRepository; 
    }

    public async Task<IEnumerable<Movie>> GetActiveMovies()
    {
        return await _repo.GetActiveMovies();
    }

    public async Task<IEnumerable<Movie>> GetByGenre(Guid genreId)
    {
        return await _repo.GetByGenre(genreId);
    }

    public async Task<IEnumerable<Movie>> GetByTitle(string title)
    {
        return await _repo.GetByTitle(title);
    }


    public async Task<IEnumerable<Movie>> GetAll()
    {
        return await _repo.GetAll();
    }

    public async Task<Movie?> GetById(Guid id)
    {
        return await _repo.GetById(id);
    }

    public async Task<Movie?> Add(Movie movie)
    {
        movie.Id = Guid.NewGuid();
        movie.IsActive = true;
        return await _repo.Add(movie);
    }

    public async Task Delete(Guid id)
    {
        var movie = await _repo.GetById(id);
        if (movie != null)
        {
            movie.IsActive = false;
            await _repo.Update(movie);
        }
    }
    public async Task Update(Movie movie)
    {
        await _repo.Update(movie);
    }

}