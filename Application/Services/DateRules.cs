using Domain.Exceptions;

namespace Application.Services;

// Los filtros por día usan el período [día 00:00, día siguiente 00:00). Con 31/12/9999 no hay "día
// siguiente": AddDays tiraría ArgumentOutOfRangeException y la API respondería 500 en vez de 400.
internal static class DateRules
{
    public static DateOnly EnsureHasNextDay(DateOnly day) =>
        day < DateOnly.MaxValue ? day : throw new BusinessRuleException("La fecha está fuera del rango permitido.");
}
