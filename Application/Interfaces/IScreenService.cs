using Application.DTOs.Screens;

namespace Application.Interfaces;

public interface IScreenService
{
    Task<IReadOnlyList<ScreenResponse>> GetAllAsync(CancellationToken ct);
    Task<ScreenResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<ScreenResponse> CreateAsync(CreateScreenRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateScreenRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
