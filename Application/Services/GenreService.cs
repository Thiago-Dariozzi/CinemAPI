using System.Globalization;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class GenreService(
    IGenreRepository genreRepository,
    IMovieRepository movieRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<IEnumerable<Genre>> GetAll()
    {
        return await genreRepository.GetAll();
    }

    public async Task<Genre?> GetById(Guid id)
    {
        return await genreRepository.GetById(id);
    }

    public async Task<Genre> Add(Genre genre)
    {
        genre.Id = Guid.NewGuid();
        genre.IsActive = true;
        genre.Name = NormalizeName(genre.Name);

        await EnsureNoDuplicate(genre.Name, excludeId: null);

        genreRepository.Add(genre);
        await unitOfWork.SaveChangesAsync();
        return genre;
    }

    public async Task Update(Genre genre)
    {
        var existing = await genreRepository.GetById(genre.Id);
        if (existing == null)
        {
            return;
        }

        var name = NormalizeName(genre.Name);
        await EnsureNoDuplicate(name, excludeId: existing.Id);

        existing.Name = name;
        existing.IsActive = genre.IsActive;
        await unitOfWork.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var genre = await genreRepository.GetById(id);
        if (genre == null)
        {
            return;
        }

        var moviesCount = await movieRepository.CountByGenre(id);
        if (moviesCount > 0)
        {
            throw new GenreConflictException(
                $"No se puede eliminar el género '{genre.Name}': tiene {moviesCount} película(s) asignada(s). " +
                "Reasigná esas películas a otro género antes de borrarlo.");
        }

        genre.IsActive = false;
        await unitOfWork.SaveChangesAsync();
    }

    // Trim + primera letra en mayúscula. No toca el resto del string (para no romper
    // nombres ya bien escritos como "Ciencia Ficción").
    private static string NormalizeName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
    }

    // Dos géneros son "el mismo" si su nombre coincide ignorando mayúsculas/minúsculas Y
    // tildes/acentos ("Acción" == "Accion" == "ACCIÓN"). CompareOptions.IgnoreNonSpace
    // ignora los diacríticos (evita tener que normalizar Unicode a mano con FormD).
    // Queda en memoria a propósito: la collation de la base distingue "Año" de "Ano" y
    // esta regla no. Los géneros son pocos y se leen sin tracking.
    private async Task EnsureNoDuplicate(string normalizedName, Guid? excludeId)
    {
        var existing = await genreRepository.GetAll();
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
