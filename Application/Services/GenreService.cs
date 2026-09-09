using System;
using System.Globalization;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public class GenreService
{
    private readonly IGenreRepository _repo;
    private readonly IMovieRepository _movieRepo;

    public GenreService(IGenreRepository genreRepository, IMovieRepository movieRepository)
    {
        _repo = genreRepository;
        _movieRepo = movieRepository;
    }

    public async Task<IEnumerable<Genre>> GetAll()
    {
        return await _repo.GetAll();
    }

    public async Task<Genre?> GetById(Guid id)
    {
        return await _repo.GetById(id);
    }

    public async Task<Genre> Add(Genre genre)
    {
        genre.Id = Guid.NewGuid();
        genre.IsActive = true;
        genre.Name = NormalizeName(genre.Name);

        await EnsureNoDuplicate(genre.Name, excludeId: null);

        return await _repo.Add(genre);
    }

    public async Task Update(Genre genre)
    {
        genre.Name = NormalizeName(genre.Name);

        await EnsureNoDuplicate(genre.Name, excludeId: genre.Id);

        await _repo.Update(genre);
    }

    public async Task Delete(Guid id)
    {
        var genre = await _repo.GetById(id);
        if (genre == null) return;

        var moviesUsingGenre = await _movieRepo.GetByGenre(id);
        var moviesCount = moviesUsingGenre.Count();
        if (moviesCount > 0)
        {
            throw new GenreConflictException(
                $"No se puede eliminar el género '{genre.Name}': tiene {moviesCount} película(s) asignada(s). " +
                "Reasigná esas películas a otro género antes de borrarlo.");
        }

        genre.IsActive = false;
        await _repo.Update(genre);
    }

    // Trim + primera letra en mayúscula. No toca el resto del string (para no romper
    // nombres ya bien escritos como "Ciencia Ficción").
    private static string NormalizeName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0) return trimmed;
        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
    }

    // Dos géneros son "el mismo" si su nombre coincide ignorando mayúsculas/minúsculas Y
    // tildes/acentos ("Acción" == "Accion" == "ACCIÓN"). CompareOptions.IgnoreNonSpace
    // ignora los diacríticos (evita tener que normalizar Unicode a mano con FormD).
    private async Task EnsureNoDuplicate(string normalizedName, Guid? excludeId)
    {
        var existing = await _repo.GetAll();
        var duplicate = existing.FirstOrDefault(g =>
            (excludeId == null || g.Id != excludeId.Value) &&
            CultureInfo.InvariantCulture.CompareInfo.Compare(
                g.Name, normalizedName, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0);

        if (duplicate != null)
        {
            throw new GenreConflictException(
                $"Ya existe un género equivalente: '{duplicate.Name}' (Id {duplicate.Id}).");
        }
    }
}
