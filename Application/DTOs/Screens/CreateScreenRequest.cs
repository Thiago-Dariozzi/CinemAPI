using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.DTOs.Screens;

public sealed record CreateScreenRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    string Name,
    [Range(1, Screen.MaxRows, ErrorMessage = "La cantidad de filas debe estar entre 1 y 26.")]
    int Rows,
    [Range(1, Screen.MaxSeatsPerRow, ErrorMessage = "Las butacas por fila deben estar entre 1 y 40.")]
    int SeatsPerRow);
