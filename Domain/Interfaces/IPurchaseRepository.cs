using Domain.Entities;
using Domain.Models;

namespace Domain.Interfaces;

// GetByIdAsync trae la compra trackeada con sus entradas (para cancelarla).
public interface IPurchaseRepository : IBaseRepository<Purchase>
{
    Task<PurchaseDetails?> GetDetailsAsync(Guid id, CancellationToken ct);

    // Las más recientes primero (por horario de la función).
    Task<IReadOnlyList<PurchaseDetails>> GetByUserAsync(Guid userId, CancellationToken ct);

    // Las más recientes primero (por fecha de compra).
    Task<IReadOnlyList<PurchaseDetails>> SearchAsync(PurchaseFilter filter, CancellationToken ct);
}
