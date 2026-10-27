using Application.DTOs.Fees;
using Application.Interfaces;
using Application.Mappings;
using Domain.Interfaces;

namespace Application.Services;

public sealed class FeeService(IFeeRepository feeRepository) : IFeeService
{
    public async Task<FeeResponse?> GetCurrentAsync(CancellationToken ct) =>
        (await feeRepository.GetCurrentAsync(ct))?.ToResponse();
}
