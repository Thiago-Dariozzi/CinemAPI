using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

// Solo el nombre: el email identifica la cuenta (y viaja en el token) y el rol lo maneja el staff.
public sealed record UpdateProfileRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Name);
