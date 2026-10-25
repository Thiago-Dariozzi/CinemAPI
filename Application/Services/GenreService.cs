using System.Globalization;
using Application.DTOs.Genres;
using Application.Interfaces;
using Application.Mappings;
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

    public async Task<IReadOnlyList<GenreResponse>> GetAllAsync(CancellationToken ct) =>
        (await genreRepository.GetAllAsync(ct)).Select(g => g.ToResponse()).ToList();

    public async Task<GenreResponse> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToResponse();

    public async Task<GenreResponse> CreateAsync(CreateGenreRequest request, CancellationToken ct)
    {
        var name = NormalizeName(request.Name);
        await EnsureNoDuplicateAsync(name, excludeId: null, ct);

        var genre = new Genre { Id = Guid.NewGuid(), Name = name, IsActive = true };
        genreRepository.Add(genre);
        await unitOfWork.SaveChangesAsync(ct);
        return genre.ToResponse();
    }

    public async Task UpdateAsync(Guid id, UpdateGenreRequest request, CancellationToken ct)
    {
        var genre = await GetEntityAsync(id, ct);

        var name = NormalizeName(request.Name);
        await EnsureNoDuplicateAsync(name, excludeId: genre.Id, ct);

        genre.Name = name;
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var genre = await GetEntityAsync(id, ct);

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

    private async Task<Genre> GetEntityAsync(Guid id, CancellationToken ct) =>
        await genreRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    // Trim + primera letra en mayúscula. No toca el resto del string (para no romper
    // nombres ya bien escritos como "Ciencia Ficción").
    private static string NormalizeName(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return trimmed;
        }

        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
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
