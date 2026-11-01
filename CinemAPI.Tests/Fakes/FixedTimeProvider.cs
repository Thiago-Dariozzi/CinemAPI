namespace CinemAPI.Tests.Fakes;

// Reloj fijo para las reglas de horario ("ya empezó", "2 horas antes"). Zona UTC: la hora local del
// servicio es exactamente Now.
public sealed class FixedTimeProvider(DateTime now) : TimeProvider
{
    public DateTime Now { get; set; } = now;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
}
