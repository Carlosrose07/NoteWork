using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Cuenta;

/// <summary>
/// Datos que envía un admin para ponerle una contraseña nueva a un usuario
/// de SU empresa (decisión de diseño: el admin escribe la clave y se la
/// comunica al usuario en persona o por mensaje).
///  - El Id NO va aquí: viaja en la dirección
///    (/api/cuenta/usuarios/{id}/password), y la Api verifica que ese
///    usuario sea de la empresa del admin.
///  - La contraseña viaja en texto solo mientras va a la Api; Identity
///    guarda únicamente su hash.
/// </summary>
public class RestablecerPasswordSolicitud
{
    // Mismas reglas que la contraseña en CrearUsuarioSolicitud. Es una primera
    // barrera rápida en el celular; Identity vuelve a validar las reglas
    // completas de complejidad (mayúscula, número, símbolo) en la Api.
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string NuevaPassword { get; set; } = string.Empty;
}