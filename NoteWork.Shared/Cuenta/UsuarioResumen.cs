namespace NoteWork.Shared.Cuenta;

/// <summary>
/// Una fila de la lista de usuarios que ve el admin.
/// La Api lo llena y el Client lo muestra.
/// OJO: no incluye contraseña, hash ni EmpresaId a propósito.
/// Esta lista solo trae usuarios de la empresa del admin, y el Client
/// no necesita saber nada más para mostrarla.
/// </summary>
public class UsuarioResumen
{
    // Identificador del usuario. El Client lo manda de vuelta para
    // editar, desactivar o restablecer la contraseña de esa fila.
    public Guid Id { get; set; }

    // Correo con el que inicia sesión. Solo se muestra: no se edita.
    public string Email { get; set; } = string.Empty;

    // Nombre para mostrar.
    public string NombreCompleto { get; set; } = string.Empty;

    // "Admin" o "Usuario".
    public string Rol { get; set; } = string.Empty;

    // false = desactivado: no puede iniciar sesión ni usar su token.
    // La lista lo usa para marcar la fila y mostrar "Desactivar" o "Reactivar".
    public bool Activo { get; set; } = true;

    // Cuándo se creó (en UTC, igual que en Usuario.cs).
    public DateTime FechaCreacion { get; set; }

    // true si el administrador actual tiene permiso para editar, desactivar
    // o restablecer la contraseña de este usuario.
    public bool PuedeGestionar { get; set; } = true;
}