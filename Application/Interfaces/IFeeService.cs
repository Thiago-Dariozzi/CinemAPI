using Application.DTOs.Fees;

namespace Application.Interfaces;

public interface IFeeService
{
    // null si no hay ningún cargo vigente.
    Task<FeeResponse?> GetCurrentAsync(CancellationToken ct);
    Task<IReadOnlyList<FeeResponse>> GetAllAsync(CancellationToken ct);
    Task<FeeResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<FeeResponse> CreateAsync(CreateFeeRequest request, CancellationToken ct);
    Task UpdateAsync(Guid id, UpdateFeeRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
