using Application.DTOs.Fees;

namespace Application.Interfaces;

public interface IFeeService
{
    // null si no hay ningún cargo vigente.
    Task<FeeResponse?> GetCurrentAsync(CancellationToken ct);
}
