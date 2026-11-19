using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Cuenta;

/// <summary>
/// Datos que envía un admin para editar un usuario de SU empresa.
/// Solo lleva el nombre a propósito (decisión de diseño):
///  - El correo es el usuario de inicio de sesión; cambiarlo trae
///    complicaciones (duplicados, sesiones), así que no se toca.
///  - El rol no se cambia desde la app, así nunca se puede dejar
///    una empresa sin admin.
///  - El Id NO va aquí: viaja en la dirección (/api/cuenta/usuarios/{id}),
///    y la Api verifica que ese usuario sea de la empresa del admin.
/// </summary>
public class EditarUsuarioSolicitud
{
    // Mismas reglas que en CrearUsuarioSolicitud, para que crear y editar
    // acepten exactamente los mismos nombres. Es una primera barrera rápida
    // en el celular; la Api vuelve a validar todo.
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;
}