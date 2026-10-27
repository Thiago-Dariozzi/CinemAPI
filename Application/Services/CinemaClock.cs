namespace Application.Services;

// Showtime.StartTime se guarda en hora local del cine (así la carga el front), sin zona horaria. Las
// reglas de "ya empezó" y "2 horas antes" comparan contra la hora local del servidor, que es la del cine.
internal static class CinemaClock
{
    public static DateTime Now(this TimeProvider timeProvider) => timeProvider.GetLocalNow().DateTime;
}
