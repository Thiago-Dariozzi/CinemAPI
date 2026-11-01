using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.DTOs.Purchases;

// Row 1 = fila "A". Que la butaca exista en la sala de la función lo valida el servicio.
public sealed record SeatRequest(
    [Range(1, Screen.MaxRows, ErrorMessage = "La fila debe estar entre 1 y 26.")]
    int Row,
    [Range(1, Screen.MaxSeatsPerRow, ErrorMessage = "El número de butaca debe estar entre 1 y 40.")]
    int Number);
