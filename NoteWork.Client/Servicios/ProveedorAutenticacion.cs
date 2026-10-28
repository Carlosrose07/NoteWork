using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace NoteWork.Client.Servicios;

// ProveedorAutenticacion (4.16): es la pieza que le responde a Blazor la
// pregunta "¿este usuario tiene sesión?". Blazor la usa para decidir si
// muestra una página protegida o te manda a /login.
// No guarda nada propio: la verdad vive en ServicioSesion (localStorage).
public class ProveedorAutenticacion : AuthenticationStateProvider, IDisposable
{
    private readonly ServicioSesion _sesion;

    // Estado "anónimo" = nadie con sesión. Se crea UNA sola vez y se reutiliza
    // (static readonly) para no gastar memoria en celulares modestos.
    private static readonly AuthenticationState Anonimo =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public ProveedorAutenticacion(ServicioSesion sesion)
    {
        _sesion = sesion;

        // Nos suscribimos al evento (4.15.14): cuando alguien inicia o cierra
        // sesión, ServicioSesion avisa y aquí le avisamos a Blazor.
        _sesion.SesionCambio += AlCambiarSesion;
    }

    // Blazor llama a este método cuando necesita saber el estado de la sesión.
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // ObtenerSesionAsync ya devuelve null si no hay sesión o si el token venció.
        var datos = await _sesion.ObtenerSesionAsync();

        if (datos is null)
        {
            return Anonimo;
        }

        // NUEVO (4.19): además de "hay sesión", le decimos a Blazor el nombre y
        // el ROL del usuario. Un "claim" es un dato sobre la persona (como una
        // etiqueta). Con el claim de rol, Blazor puede decidir con
        // [Authorize(Roles = "Admin")] quién ve cada página.
        // Son solo 2 etiquetas pequeñas, tomadas de lo que ya está en la sesión
        // (no hacemos ninguna petición extra a la Api).
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, datos.Email)
        };

        // Solo agregamos el rol si viene con texto (evita una etiqueta vacía).
        if (!string.IsNullOrWhiteSpace(datos.Rol))
        {
            claims.Add(new Claim(ClaimTypes.Role, datos.Rol));
        }

        // El texto "sesion" como tipo de autenticación hace que Blazor la
        // considere autenticada (igual que antes).
        var identidad = new ClaimsIdentity(claims, authenticationType: "sesion");
        return new AuthenticationState(new ClaimsPrincipal(identidad));
    }

    // Se ejecuta cada vez que alguien entra o sale. Le avisa a Blazor que
    // vuelva a preguntar el estado, para que las páginas reaccionen solas.
    private void AlCambiarSesion()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    // Nos damos de baja del evento para no dejar suscripciones colgadas
    // (gasta memoria). Blazor llama a Dispose solo al cerrar la app.
    public void Dispose()
    {
        _sesion.SesionCambio -= AlCambiarSesion;
    }
}