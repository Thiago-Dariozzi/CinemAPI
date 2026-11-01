using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

public sealed record LoginRequest(
    [Required(ErrorMessage = "El email es obligatorio.")]
    string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    string Password);
