using System.Globalization;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Application.Services;

public sealed class GenreService(
    IGenreRepository genreRepository,
    IMovieRepository movieRepository,
    IUnitOfWork unitOfWork) : IGenreService
{
    private const string EntityName = "el género";

    public Task<IReadOnlyList<Genre>> GetAllAsync(CancellationToken ct) => genreRepository.GetAllAsync(ct);

    public async Task<Genre> GetByIdAsync(Guid id, CancellationToken ct) =>
        await genreRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    public async Task<Genre> AddAsync(Genre genre, CancellationToken ct)
    {
        genre.Id = Guid.NewGuid();
        genre.IsActive = true;
        genre.Name = NormalizeName(genre.Name);

        await EnsureNoDuplicateAsync(genre.Name, excludeId: null, ct);

        genreRepository.Add(genre);
        await unitOfWork.SaveChangesAsync(ct);
        return genre;
    }

    public async Task UpdateAsync(Genre genre, CancellationToken ct)
    {
        var existing = await GetByIdAsync(genre.Id, ct);

        var name = NormalizeName(genre.Name);
        await EnsureNoDuplicateAsync(name, excludeId: existing.Id, ct);

        existing.Name = name;
        existing.IsActive = genre.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var genre = await GetByIdAsync(id, ct);

        var moviesCount = await movieRepository.CountByGenreAsync(id, ct);
        if (moviesCount > 0)
        {
            throw new GenreConflictException(
                $"No se puede eliminar el género '{genre.Name}': tiene {moviesCount} película(s) asignada(s). " +
                "Reasigná esas películas a otro género antes de borrarlo.");
        }

        genre.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);
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
    private async Task EnsureNoDuplicateAsync(string normalizedName, Guid? excludeId, CancellationToken ct)
    {
        var existing = await genreRepository.GetAllAsync(ct);
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
