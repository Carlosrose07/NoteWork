using Microsoft.AspNetCore.Identity;

// IMPORTANTE: este namespace debe ser IGUAL al que está arriba en AppDbContext.cs.
namespace NoteWork.Api.Datos;

/// <summary>
/// Usuario del sistema (quien inicia sesión).
/// Hereda de IdentityUser<Guid>, así que YA trae: Id, UserName, Email,
/// PasswordHash, PhoneNumber, etc. Solo agregamos lo propio de NoteWork.
///
/// Vive en la Api (no en Shared) a propósito: así el Client (el celular)
/// no descarga código de Identity y la app pesa menos.
/// </summary>
public class Usuario : IdentityUser<Guid>
{
    /// <summary>
    /// A qué empresa pertenece este usuario. Es la base del sistema multiempresa:
    /// cada consulta se filtrará por este valor para que una empresa
    /// nunca vea los datos de otra.
    /// </summary>
    public Guid EmpresaId { get; set; }

    /// <summary>Nombre para mostrar en pantalla (ej. "Juan Pérez").</summary>
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Cuándo se creó el usuario. Se guarda en UTC (hora universal)
    /// para evitar líos de zona horaria entre servidor y celulares.
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// NUEVO (Bloque B): false = el admin desactivó a este usuario.
    /// Un usuario desactivado no puede iniciar sesión, y la Api rechaza
    /// su token en cada petición (así la sesión abierta también se corta).
    /// Es un campo propio, separado de LockoutEnd de Identity, que se usa
    /// para los intentos fallidos de contraseña: así no se mezclan las
    /// dos cosas y nadie queda reactivado por error.
    /// Por defecto true: los usuarios que ya existen quedan activos.
    /// Se desactiva en lugar de borrar porque UsuarioId en proyectos y
    /// clientes no tiene clave foránea: borrar dejaría datos huérfanos.
    /// </summary>
    public bool Activo { get; set; } = true;
}