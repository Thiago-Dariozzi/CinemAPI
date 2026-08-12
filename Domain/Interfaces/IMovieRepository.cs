using System;
using Domain.Entities;
using Domain.Interfaces;

namespace Domain.Interfaces;

public interface IMovieRepository : IBaseRepository<Movie>
{
    Task<IEnumerable<Movie>> GetByTitle(string title);
    Task<IEnumerable<Movie>> GetActiveMovies();
    Task<IEnumerable<Movie>> GetByGenre(string genre);
}