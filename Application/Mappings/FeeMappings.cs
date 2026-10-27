using Application.DTOs.Fees;
using Domain.Entities;

namespace Application.Mappings;

public static class FeeMappings
{
    public static FeeResponse ToResponse(this Fee fee) =>
        new(fee.Id, fee.Name, fee.Percentage, fee.FixedAmount, fee.IsCurrent, fee.IsActive);
}
