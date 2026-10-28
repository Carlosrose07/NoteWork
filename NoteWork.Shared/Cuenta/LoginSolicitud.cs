using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Cuenta;

// Datos que el Client envía al hacer login.
// Es un "record": una clase corta pensada solo para transportar datos.
// Los atributos [Required] y [EmailAddress] permiten validar el formulario
// en el Client (sin viajar al servidor) y también en la Api.
public record LoginSolicitud
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; init; } = string.Empty;
}