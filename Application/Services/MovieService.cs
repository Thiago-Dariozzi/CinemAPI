using Application.DTOs.Movies;
using Application.Interfaces;
using Application.Mappings;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class MovieService(IMovieRepository movieRepository, IUnitOfWork unitOfWork) : IMovieService
{
    private const string EntityName = "la película";

    public async Task<IReadOnlyList<MovieResponse>> GetActiveMoviesAsync(CancellationToken ct) =>
        ToResponses(await movieRepository.GetActiveMoviesAsync(ct));

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
        movie.Id = Guid.NewGuid();
        movie.IsActive = true;
        movieRepository.Add(movie);
        await unitOfWork.SaveChangesAsync(ct);
        return movie.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateMovieRequest request, CancellationToken ct)
    {
        var movie = await GetEntityAsync(id, ct);
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

    private static List<MovieResponse> ToResponses(IEnumerable<Movie> movies) =>
        movies.Select(m => m.ToResponse()).ToList();
}
