using System.Net.Http.Json;
using NoteWork.Shared.Clientes;
using NoteWork.Shared.Comun;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Servicio que comunica las pantallas del Client con los endpoints de clientes en la Api.
/// Usa el HttpClient configurado con ManejadorToken, por lo que el token viaja solo.
/// Ningún método lanza excepciones: si algo falla, deja el mensaje legible en
/// <see cref="UltimoError"/> (hallazgos 3.4 y 3.13).
/// </summary>
public class ServicioClientes
{
    private readonly HttpClient _http;

    public ServicioClientes(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Mensaje legible del último error ocurrido en este servicio.
    /// Las pantallas lo leen cuando un método devuelve null o false.
    /// Se limpia al inicio de cada llamada.
    /// </summary>
    public string? UltimoError { get; private set; }

    /// <summary>
    /// Obtiene la lista paginada de clientes. Permite filtrar por término de búsqueda.
    /// Si falla, devuelve una página vacía (nunca null) y deja el motivo en UltimoError.
    /// </summary>
    public async Task<PaginaResultado<ClienteResumen>> ObtenerClientesAsync(int pagina = 1, int tamano = 20, string? buscar = null)
    {
        var url = $"/api/clientes?pagina={pagina}&tamano={tamano}";
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            url += $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
        }

        var resultado = await LeerAsync<PaginaResultado<ClienteResumen>>(url, "No se pudo cargar la lista de clientes.");
        return resultado ?? new PaginaResultado<ClienteResumen>([], 0, pagina, tamano);
    }

    /// <summary>
    /// Obtiene los datos de un cliente por su Id para cargarlo en el formulario de edición.
    /// Devuelve null si no existe o si falla (el motivo queda en UltimoError).
    /// </summary>
    public async Task<ClienteGuardarSolicitud?> ObtenerPorIdAsync(Guid id)
    {
        return await LeerAsync<ClienteGuardarSolicitud>($"/api/clientes/{id}", "No se pudo cargar el cliente.");
    }

    /// <summary>
    /// Envía un nuevo cliente para guardarlo en la base de datos.
    /// Devuelve (true, null) si fue exitoso o (false, mensaje legible) si hubo error.
    /// </summary>
    public async Task<(bool Exito, string? Error)> CrearAsync(ClienteGuardarSolicitud solicitud)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsJsonAsync("/api/clientes", solicitud);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            // Antes se mostraba el texto crudo de la Api; ahora pasa por ErroresApi.
            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo crear el cliente.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    /// <summary>
    /// Actualiza los datos de un cliente existente.
    /// </summary>
    public async Task<(bool Exito, string? Error)> EditarAsync(Guid id, ClienteGuardarSolicitud solicitud)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync($"/api/clientes/{id}", solicitud);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo actualizar el cliente.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    /// <summary>
    /// Solicita el borrado lógico del cliente.
    /// Devuelve false si falla (el motivo queda en UltimoError).
    /// </summary>
    public async Task<bool> BorrarAsync(Guid id)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.DeleteAsync($"/api/clientes/{id}");
            if (respuesta.IsSuccessStatusCode) return true;

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo borrar el cliente.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return false;
    }

    /// <summary>
    /// GET genérico que nunca lanza excepción. Si falla, guarda el mensaje en UltimoError
    /// y devuelve null. Usa GetAsync (y no GetFromJsonAsync) para poder leer el código
    /// de estado y el cuerpo del error.
    /// </summary>
    private async Task<T?> LeerAsync<T>(string url, string mensajePorDefecto) where T : class
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.GetAsync(url);
            if (!respuesta.IsSuccessStatusCode)
            {
                UltimoError = await ErroresApi.LeerAsync(respuesta, mensajePorDefecto);
                return null;
            }

            return await respuesta.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
            return null;
        }
    }
}