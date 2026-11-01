using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

public sealed record ChangePasswordRequest(
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    string CurrentPassword,
    [Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña nueva debe tener entre 8 y 100 caracteres.")]
    string NewPassword);
