using Application.DTOs.Fees;
using Domain.Entities;

namespace Application.Mappings;

public static class FeeMappings
{
    public static FeeResponse ToResponse(this Fee fee) =>
        new(fee.Id, fee.Name, fee.Percentage, fee.FixedAmount, fee.IsCurrent, fee.IsActive);

    // IsCurrent lo maneja el servicio, que primero desmarca al vigente anterior.
    public static Fee ToEntity(this CreateFeeRequest request) =>
        new()
        {
            Name = request.Name.Trim(),
            Percentage = request.Percentage.GetValueOrDefault(),
            FixedAmount = request.FixedAmount.GetValueOrDefault(),
        };

    public static void ApplyTo(this UpdateFeeRequest request, Fee fee)
    {
        fee.Name = request.Name.Trim();
        fee.Percentage = request.Percentage.GetValueOrDefault();
        fee.FixedAmount = request.FixedAmount.GetValueOrDefault();
    }
}
