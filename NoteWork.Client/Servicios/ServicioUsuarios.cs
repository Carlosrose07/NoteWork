using System.Net;
using System.Net.Http.Json;
using NoteWork.Shared.Cuenta;      // CrearUsuarioSolicitud, UsuarioResumen, etc. (viven en Shared)

namespace NoteWork.Client.Servicios;

// ServicioUsuarios (4.19, ampliado en el Bloque B): se encarga de hablar con la Api
// para crear y GESTIONAR usuarios (listar, editar nombre, desactivar, reactivar y
// restablecer contraseña).
// Igual que ServicioSesion, las pantallas no llaman a la Api directamente:
// le piden las cosas a este servicio, y así el código de red vive en un solo lugar.
//
// Todos los métodos devuelven un "resultado" en vez de lanzar errores:
// (true, ...) si salió bien o (false, "mensaje") para mostrárselo a quien usa la pantalla.
// Los mensajes de error los arma ErroresApi, el único lugar que decide qué texto
// se muestra (así no hay dos formas distintas de leer los errores de la Api).
public class ServicioUsuarios
{
    // Mensaje propio para usuarios cuando la Api responde 404. Lo ponemos AQUÍ y no en
    // ErroresApi para no cambiar los mensajes de clientes, proyectos y partidas.
    private const string UsuarioNoEncontrado =
        "No se encontró ese usuario. Es posible que ya no exista o que no sea de tu empresa.";

    // Es el MISMO HttpClient compartido de Program.cs. Ya trae a ManejadorToken,
    // así que el token del admin se agrega solo a cada petición.
    private readonly HttpClient _http;

    public ServicioUsuarios(HttpClient http)
    {
        _http = http;
    }

    // ------------------------------------------------------------------
    // CREAR (ya existía; ahora usa ErroresApi para leer los errores)
    // ------------------------------------------------------------------
    // Pide a la Api crear un usuario. Devuelve (true, null) si salió bien, o
    // (false, "mensaje") para mostrárselo a quien está usando la pantalla.
    public Task<(bool Exito, string? Error)> CrearUsuarioAsync(CrearUsuarioSolicitud solicitud)
        => EnviarAsync(
            // POST a la Api. Aquí NO ponemos el token: lo agrega ManejadorToken.
            () => _http.PostAsJsonAsync("api/cuenta/usuarios", solicitud),
            "No se pudo crear el usuario. Revisa el correo y la contraseña.");

    // ------------------------------------------------------------------
    // LISTAR
    // ------------------------------------------------------------------
    // Trae los usuarios de la empresa del admin en UNA sola llamada. La Api manda solo
    // lo necesario (nombre, correo, rol, estado y fecha): cero contraseñas y respuesta
    // pequeña, pensado para celulares modestos.
    public async Task<(bool Exito, List<UsuarioResumen>? Usuarios, string? Error)> ListarAsync()
    {
        try
        {
            using var respuesta = await _http.GetAsync("api/cuenta/usuarios");

            if (!respuesta.IsSuccessStatusCode)
            {
                return (false, null,
                    await ErroresApi.LeerAsync(respuesta, "No se pudo cargar la lista de usuarios."));
            }

            var usuarios = await respuesta.Content.ReadFromJsonAsync<List<UsuarioResumen>>();
            return (true, usuarios ?? new List<UsuarioResumen>(), null);
        }
        catch (Exception ex)
        {
            // Sin red, tiempo agotado o JSON inesperado: ErroresApi lo convierte en texto legible.
            return (false, null, ErroresApi.DeExcepcion(ex));
        }
    }

    // ------------------------------------------------------------------
    // EDITAR NOMBRE Y ROL
    // ------------------------------------------------------------------
    // Cambia el nombre y el rol ("Usuario" o "Admin"). El correo no se toca.
    // La Api valida los datos y que nunca quede la empresa sin un Admin activo.
    public Task<(bool Exito, string? Error)> EditarNombreAsync(Guid id, string nombreCompleto, string rol = "Usuario")
        => EnviarAsync(
            () => _http.PutAsJsonAsync(
                $"api/cuenta/usuarios/{id}",
                new EditarUsuarioSolicitud { NombreCompleto = nombreCompleto, Rol = rol }),
            "No se pudo guardar los cambios. Inténtalo de nuevo.");

    // ------------------------------------------------------------------
    // DESACTIVAR / REACTIVAR
    // ------------------------------------------------------------------
    // Desactivar NO borra al usuario: solo le impide entrar (y corta su sesión abierta
    // a los pocos segundos). Si el admin intenta desactivarse a sí mismo, la Api responde
    // 400 con "No puedes desactivarte a ti mismo." y ese texto llega tal cual a la pantalla.
    public Task<(bool Exito, string? Error)> DesactivarAsync(Guid id)
        => EnviarAsync(
            () => _http.PostAsync($"api/cuenta/usuarios/{id}/desactivar", content: null),
            "No se pudo desactivar al usuario. Inténtalo de nuevo.");

    public Task<(bool Exito, string? Error)> ReactivarAsync(Guid id)
        => EnviarAsync(
            () => _http.PostAsync($"api/cuenta/usuarios/{id}/reactivar", content: null),
            "No se pudo reactivar al usuario. Inténtalo de nuevo.");

    // ------------------------------------------------------------------
    // RESTABLECER CONTRASEÑA
    // ------------------------------------------------------------------
    // El admin escribe una contraseña nueva y se la comunica al usuario. Si no cumple las
    // reglas de seguridad, la Api devuelve el motivo y aquí se muestra tal cual.
    public Task<(bool Exito, string? Error)> RestablecerPasswordAsync(Guid id, string nuevaPassword)
        => EnviarAsync(
            () => _http.PostAsJsonAsync(
                $"api/cuenta/usuarios/{id}/password",
                new RestablecerPasswordSolicitud { NuevaPassword = nuevaPassword }),
            "No se pudo cambiar la contraseña. Revisa que cumpla las reglas.");

    // ------------------------------------------------------------------
    // AYUDANTE COMÚN
    // ------------------------------------------------------------------
    // Todas las acciones que no devuelven datos (crear, editar, desactivar, reactivar y
    // restablecer) hacen lo mismo: enviar, revisar si salió bien y, si no, armar el mensaje.
    // Por eso viven en un solo método: una sola forma de manejar errores, sin copiar código.
    // Recibe la petición como una función para que el try/catch cubra también el envío.
    private static async Task<(bool Exito, string? Error)> EnviarAsync(
        Func<Task<HttpResponseMessage>> enviar,
        string mensajePorDefecto)
    {
        try
        {
            using var respuesta = await enviar();

            if (respuesta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            // 404: mensaje propio de usuarios (ver la constante de arriba).
            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, UsuarioNoEncontrado);
            }

            // Resto de errores (401, 403, 400 con detalle, 5xx...): los lee ErroresApi.
            return (false, await ErroresApi.LeerAsync(respuesta, mensajePorDefecto));
        }
        catch (Exception ex)
        {
            // Sin red, tiempo agotado, etc.
            return (false, ErroresApi.DeExcepcion(ex));
        }
    }
}