using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NoteWork.Shared.Cuenta;      // CrearUsuarioSolicitud (vive en Shared)

namespace NoteWork.Client.Servicios;

// ServicioUsuarios (4.19): se encarga de hablar con la Api para crear usuarios.
// Igual que ServicioSesion, las pantallas no llaman a la Api directamente:
// le piden las cosas a este servicio, y así el código de red vive en un solo lugar.
public class ServicioUsuarios
{
    // Es el MISMO HttpClient compartido de Program.cs. Ya trae a ManejadorToken,
    // así que el token del admin se agrega solo a cada petición.
    private readonly HttpClient _http;

    public ServicioUsuarios(HttpClient http)
    {
        _http = http;
    }

    // Pide a la Api crear un usuario. Devuelve (true, null) si salió bien, o
    // (false, "mensaje") para mostrárselo a quien está usando la pantalla.
    public async Task<(bool Exito, string? Error)> CrearUsuarioAsync(CrearUsuarioSolicitud solicitud)
    {
        try
        {
            // POST a la Api. Aquí NO ponemos el token: lo agrega ManejadorToken.
            var respuesta = await _http.PostAsJsonAsync("api/cuenta/usuarios", solicitud);

            // 200: el usuario quedó creado.
            if (respuesta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            // Traducimos los códigos de la Api a mensajes claros.
            switch (respuesta.StatusCode)
            {
                case HttpStatusCode.Unauthorized:   // 401: no hay sesión o el token venció
                    return (false, "Tu sesión venció. Cierra sesión y vuelve a entrar.");

                case HttpStatusCode.Forbidden:      // 403: hay sesión, pero no es Admin
                    return (false, "No tienes permiso para crear usuarios.");

                case HttpStatusCode.BadRequest:     // 400: datos no válidos (ej. contraseña floja o correo repetido)
                    return (false, await LeerErroresAsync(respuesta));

                default:
                    return (false, "No se pudo crear el usuario. Intenta de nuevo.");
            }
        }
        catch (HttpRequestException)
        {
            // No hubo respuesta: Api apagada, sin internet o CORS.
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    // La Api responde los errores de validación como JSON con una sección
    // "errors". Aquí sacamos solo los textos y los juntamos en un mensaje.
    // Leemos el JSON directo con JsonDocument (viene con .NET): es más liviano
    // que crear una clase solo para esto.
    private static async Task<string> LeerErroresAsync(HttpResponseMessage respuesta)
    {
        try
        {
            // Leemos desde el "stream" para no copiar todo el texto en memoria.
            await using var contenido = await respuesta.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(contenido);

            if (documento.RootElement.TryGetProperty("errors", out var errores)
                && errores.ValueKind == JsonValueKind.Object)
            {
                var mensajes = new List<string>();

                foreach (var grupo in errores.EnumerateObject())
                {
                    if (grupo.Value.ValueKind != JsonValueKind.Array) continue;

                    foreach (var mensaje in grupo.Value.EnumerateArray())
                    {
                        var texto = mensaje.GetString();
                        if (!string.IsNullOrWhiteSpace(texto)) mensajes.Add(texto);
                    }
                }

                if (mensajes.Count > 0)
                {
                    return string.Join(" ", mensajes);
                }
            }
        }
        catch (JsonException)
        {
            // Si el texto no era JSON válido, caemos al mensaje genérico de abajo.
        }

        return "Los datos no son válidos. Revisa el correo y la contraseña.";
    }
}