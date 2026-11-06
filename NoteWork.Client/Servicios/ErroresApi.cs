using System.Net;
using System.Text.Json;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Convierte las respuestas de error de la Api y las excepciones de red en mensajes
/// legibles para el usuario. Es el ÚNICO lugar que decide qué texto se muestra
/// (hallazgos 3.1, 3.4 y 3.13), así los servicios y las pantallas no repiten esta lógica.
/// </summary>
public static class ErroresApi
{
    // Mensajes fijos reutilizables. Las pantallas pueden compararlos para, por ejemplo,
    // mandar a /login cuando la sesión venció.
    public const string SinConexion =
        "No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo. " +
        "Si la app estuvo un rato sin usarse, puede tardar hasta un minuto en despertar.";

    public const string TiempoAgotado =
        "El servidor tardó demasiado en responder. Inténtalo de nuevo en unos segundos.";

    public const string SesionVencida =
        "Tu sesión venció. Inicia sesión de nuevo.";

    /// <summary>
    /// Lee una respuesta HTTP que NO fue exitosa y devuelve un mensaje legible.
    /// Si no puede interpretar el cuerpo, devuelve <paramref name="mensajePorDefecto"/>.
    /// </summary>
    public static async Task<string> LeerAsync(HttpResponseMessage respuesta, string mensajePorDefecto)
    {
        // 1) Códigos que tienen un mensaje propio, sin importar el cuerpo.
        switch (respuesta.StatusCode)
        {
            case HttpStatusCode.Unauthorized:
                return SesionVencida;
            case HttpStatusCode.Forbidden:
                return "No tienes permiso para hacer esto.";
            case HttpStatusCode.NotFound:
                return "No se encontró lo que buscabas. Es posible que haya sido eliminado.";
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                // Render gratuito responde así mientras despierta.
                return "El servidor está despertando. Espera unos segundos e inténtalo de nuevo.";
        }

        if ((int)respuesta.StatusCode >= 500)
        {
            return "El servidor tuvo un problema. Inténtalo de nuevo; si sigue igual, avisa al administrador.";
        }

        // 2) Errores 4xx con detalle (por ejemplo 400): intentamos sacar el texto real.
        string cuerpo;
        try
        {
            cuerpo = await respuesta.Content.ReadAsStringAsync();
        }
        catch
        {
            return mensajePorDefecto;
        }

        return ExtraerTexto(cuerpo) ?? mensajePorDefecto;
    }

    /// <summary>
    /// Convierte una excepción (sin red, tiempo agotado, JSON inválido) en un mensaje legible.
    /// </summary>
    public static string DeExcepcion(Exception ex) => ex switch
    {
        TaskCanceledException => TiempoAgotado,
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => SesionVencida,
        HttpRequestException { StatusCode: HttpStatusCode.NotFound } =>
            "No se encontró lo que buscabas. Es posible que haya sido eliminado.",
        HttpRequestException => SinConexion,
        JsonException => "La respuesta del servidor no tiene el formato esperado.",
        _ => "Ocurrió un error inesperado. Inténtalo de nuevo."
    };

    /// <summary>
    /// La Api responde errores en 3 formatos (hallazgo 2.11): { "mensaje": "..." },
    /// ValidationProblem ({ "errors": { campo: [mensajes] } }) y texto plano.
    /// Esta función entiende los tres y devuelve solo el texto útil.
    /// </summary>
    private static string? ExtraerTexto(string cuerpo)
    {
        cuerpo = cuerpo.Trim();
        if (cuerpo.Length == 0) return null;

        try
        {
            if (cuerpo.StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(cuerpo);
                var raiz = doc.RootElement;

                // Formato 1: { "mensaje": "..." }
                var mensaje = TextoDe(raiz, "mensaje");
                if (mensaje is not null) return mensaje;

                // Formato 2: ValidationProblem con lista de errores por campo.
                if (TryGet(raiz, "errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                {
                    var mensajes = new List<string>();
                    foreach (var campo in errores.EnumerateObject())
                    {
                        if (campo.Value.ValueKind != JsonValueKind.Array) continue;
                        foreach (var m in campo.Value.EnumerateArray())
                        {
                            var texto = m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(texto)) mensajes.Add(texto);
                        }
                    }
                    if (mensajes.Count > 0) return string.Join(" ", mensajes.Distinct());
                }

                // Formato 3: Problem estándar (detail o title).
                return TextoDe(raiz, "detail") ?? TextoDe(raiz, "title");
            }

            // Texto plano que llegó entre comillas JSON: "Mensaje" -> Mensaje
            if (cuerpo.StartsWith('"'))
            {
                return JsonSerializer.Deserialize<string>(cuerpo);
            }
        }
        catch (JsonException)
        {
            return null; // Si no se puede leer, usamos el mensaje por defecto.
        }

        // Texto plano normal. Se ignora si parece una página HTML o es demasiado largo.
        if (cuerpo.StartsWith('<') || cuerpo.Length > 300) return null;
        return cuerpo;
    }

    // Busca una propiedad JSON sin distinguir mayúsculas de minúsculas.
    private static bool TryGet(JsonElement objeto, string nombre, out JsonElement valor)
    {
        foreach (var p in objeto.EnumerateObject())
        {
            if (string.Equals(p.Name, nombre, StringComparison.OrdinalIgnoreCase))
            {
                valor = p.Value;
                return true;
            }
        }
        valor = default;
        return false;
    }

    // Devuelve el texto de una propiedad JSON si existe, es texto y no está vacía.
    private static string? TextoDe(JsonElement objeto, string nombre) =>
        TryGet(objeto, nombre, out var v)
        && v.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(v.GetString())
            ? v.GetString()
            : null;
}