using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

// Sin Role: el registro público siempre crea un cliente.
public sealed record RegisterRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Name,
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    string Password);
