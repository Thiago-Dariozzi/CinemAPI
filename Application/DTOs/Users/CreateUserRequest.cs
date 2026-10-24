using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Users;

public sealed record CreateUserRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Name,
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    string Password,
    [StringLength(30, ErrorMessage = "El rol no puede superar los 30 caracteres.")]
    string? Role);
