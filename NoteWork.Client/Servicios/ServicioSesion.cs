using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NoteWork.Shared.Cuenta;      // LoginSolicitud y LoginRespuesta (viven en Shared)

namespace NoteWork.Client.Servicios;

// ServicioSesion (4.15.7): se encarga de TODO lo relacionado con la sesión:
// iniciar sesión, recordarla y cerrarla. Las pantallas no hablan con la Api ni
// con el navegador directamente; le piden las cosas a este servicio. Así, si
// algún día cambia cómo se guarda el token, se cambia SOLO este archivo.
public class ServicioSesion
{
    // Nombre con el que se guarda la sesión en localStorage del navegador.
    // Lleva el prefijo "notework." para no chocar con otras apps del mismo sitio.
    private const string ClaveSesion = "notework.sesion";

    private readonly HttpClient _http;      // el que apunta a la Api (Program.cs)
    private readonly IJSRuntime _js;        // permite hablar con el navegador

    // NUEVO (4.15.14): un "evento" es un aviso al que otras partes de la app
    // pueden suscribirse. Aquí avisamos "la sesión cambió" (alguien entró o
    // salió) para que el menú se actualice solo, sin recargar la página.
    // Es liviano: si nadie está suscrito, no hace nada.
    public event Action? SesionCambio;

    // Blazor entrega estas dos herramientas solo (se llama "inyección de
    // dependencias"; las registraremos en Program.cs en el siguiente paso).
    public ServicioSesion(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    // Intenta iniciar sesión. Devuelve (true, null) si salió bien, o
    // (false, "mensaje") para mostrarle al usuario qué pasó.
    public async Task<(bool Exito, string? Error)> IniciarSesionAsync(LoginSolicitud solicitud)
    {
        try
        {
            // POST a la Api: convierte la solicitud a JSON automáticamente.
            var respuesta = await _http.PostAsJsonAsync("api/cuenta/login", solicitud);

            // 401 = la Api respondió, pero correo o contraseña no coinciden.
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (false, "Correo o contraseña incorrectos.");
            }

            // Cualquier otro fallo (400, 500...): mensaje genérico, sin detalles
            // técnicos que no le sirven al usuario.
            if (!respuesta.IsSuccessStatusCode)
            {
                return (false, "No se pudo iniciar sesión. Intenta de nuevo.");
            }

            var datos = await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>();

            if (datos is null || string.IsNullOrWhiteSpace(datos.AccessToken))
            {
                return (false, "La respuesta del servidor no es válida.");
            }

            // DECISIÓN 4.15.2: guardar la sesión en localStorage (sigue ahí
            // aunque se cierre el navegador). Se guarda UN solo texto JSON con
            // todo: es una sola escritura, liviana para celulares modestos.
            // "localStorage.setItem" se llama directo, sin escribir JavaScript.
            var json = JsonSerializer.Serialize(datos);
            await _js.InvokeVoidAsync("localStorage.setItem", ClaveSesion, json);

            // NUEVO (4.15.14): avisar que ya hay sesión. El "?." significa
            // "solo si alguien está suscrito".
            SesionCambio?.Invoke();

            return (true, null);
        }
        catch (HttpRequestException)
        {
            // No hubo respuesta: la Api está apagada, no hay internet, o CORS
            // bloqueó la petición. Al usuario le basta saber que no hay conexión.
            return (false, "No se pudo conectar con el servidor.");
        }
    }

    // Devuelve la sesión guardada, o null si no hay sesión (o ya venció).
    public async Task<LoginRespuesta?> ObtenerSesionAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", ClaveSesion);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        LoginRespuesta? sesion;
        try
        {
            sesion = JsonSerializer.Deserialize<LoginRespuesta>(json);
        }
        catch (JsonException)
        {
            // El texto guardado está dañado: se borra para no quedar trabado.
            await CerrarSesionAsync();
            return null;
        }

        // Si el token ya venció, de nada sirve: se borra y se pide login de nuevo.
        if (sesion is null || sesion.ExpiraEn <= DateTime.UtcNow)
        {
            await CerrarSesionAsync();
            return null;
        }

        return sesion;
    }

    // Cerrar sesión = borrar lo guardado en el navegador.
    public async Task CerrarSesionAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", ClaveSesion);

        // NUEVO (4.15.14): avisar que la sesión ya no existe.
        SesionCambio?.Invoke();
    }
}