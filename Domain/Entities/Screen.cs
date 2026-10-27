using Domain.Interfaces;

namespace Domain.Entities;

// Las butacas se identifican por fila (1 = "A") y número dentro de la fila. La sala es un rectángulo
// Rows × SeatsPerRow: no hay butacas faltantes ni pasillos.
public class Screen : IEntity
{
    public const int MaxRows = 26;
    public const int MaxSeatsPerRow = 40;

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Rows { get; set; }
    public int SeatsPerRow { get; set; }

    // Calculada: no se guarda en la base.
    public int Capacity => Rows * SeatsPerRow;

    public bool IsActive { get; set; } = true;

    public bool HasSeat(int row, int number) => row >= 1 && row <= Rows && number >= 1 && number <= SeatsPerRow;
}
