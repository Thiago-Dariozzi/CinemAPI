using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Reports;

public sealed record OccupancyReportRequest(
    [Required(ErrorMessage = "Indicá el día (date).")]
    DateOnly? Date);
