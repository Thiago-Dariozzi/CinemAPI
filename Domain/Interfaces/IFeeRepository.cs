using Domain.Entities;

namespace Domain.Interfaces;

public interface IFeeRepository : IBaseRepository<Fee>
{
    // El cargo que se aplica a las compras nuevas, o null si no hay ninguno vigente.
    Task<Fee?> GetCurrentAsync(CancellationToken ct);
}
