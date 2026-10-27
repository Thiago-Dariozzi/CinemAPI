using Application.DTOs.Purchases;

namespace Application.Interfaces;

public interface IPurchaseService
{
    Task<PurchaseResponse> CreateAsync(CreatePurchaseRequest request, CancellationToken ct);
    Task<IReadOnlyList<PurchaseResponse>> GetMineAsync(CancellationToken ct);
    Task<PurchaseResponse> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<PurchaseResponse>> SearchAsync(PurchaseSearchRequest request, CancellationToken ct);
    Task<PurchaseResponse> CancelMineAsync(Guid id, CancellationToken ct);
    Task<PurchaseResponse> CancelAnyAsync(Guid id, CancellationToken ct);
}
