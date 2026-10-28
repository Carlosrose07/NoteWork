namespace NoteWork.Shared.Cuenta;

// Datos que la Api devuelve cuando el login es correcto.
// El Client guardará el AccessToken y lo enviará en cada petición
// para demostrar quién es (así no hay cookies, que es más liviano en PWA).
public record LoginRespuesta
{
    // Token de acceso: es el "pase" que el Client presenta a la Api.
    public string AccessToken { get; init; } = string.Empty;

    // Cuándo vence el token (en UTC). El Client lo usa para saber
    // si debe pedirle al usuario que inicie sesión otra vez.
    public DateTime ExpiraEn { get; init; }

    // Datos solo para mostrar en pantalla (ej. "Hola, Carlos").
    public string NombreCompleto { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    // Rol del usuario (ej. "Admin"); el Client lo usará para mostrar
    // u ocultar opciones. La seguridad real siempre se valida en la Api.
    public string Rol { get; init; } = string.Empty;
}