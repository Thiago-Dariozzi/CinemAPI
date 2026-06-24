using System;
using Domain.Entities;

namespace Domain.Inerfaces;

public interface IMovieRepository : IBaseRepository<Movie>
{
    Task<IEnumerable<Movie>> GetByTitle(string title);
    Task<IEnumerable<Movie>> GetActiveMovies();
    Task<IEnumerable<Movie>> GetByGenre(string genre);
}