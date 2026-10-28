using System.Net.Http.Json;
using NoteWork.Shared.Clientes;
using NoteWork.Shared.Comun;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Servicio que comunica las pantallas del Client con los endpoints de clientes en la Api.
/// Usa el HttpClient configurado con ManejadorToken, por lo que el token viaja solo.
/// </summary>
public class ServicioClientes
{
    private readonly HttpClient _http;

    public ServicioClientes(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Obtiene la lista paginada de clientes. Permite filtrar por término de búsqueda.
    /// </summary>
    public async Task<PaginaResultado<ClienteResumen>> ObtenerClientesAsync(int pagina = 1, int tamano = 20, string? buscar = null)
    {
        var url = $"/api/clientes?pagina={pagina}&tamano={tamano}";
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            url += $"&buscar={Uri.EscapeDataString(buscar.Trim())}";
        }

        var resultado = await _http.GetFromJsonAsync<PaginaResultado<ClienteResumen>>(url);
        return resultado ?? new PaginaResultado<ClienteResumen>([], 0, pagina, tamano);
    }

    /// <summary>
    /// Obtiene los datos de un cliente por su Id para cargarlo en el formulario de edición.
    /// </summary>
    public async Task<ClienteGuardarSolicitud?> ObtenerPorIdAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ClienteGuardarSolicitud>($"/api/clientes/{id}");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Envía un nuevo cliente para guardarlo en la base de datos.
    /// Devuelve true si fue exitoso o false si hubo error.
    /// </summary>
    public async Task<(bool Exito, string? Error)> CrearAsync(ClienteGuardarSolicitud solicitud)
    {
        var respuesta = await _http.PostAsJsonAsync("/api/clientes", solicitud);

        if (respuesta.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var error = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "Error al crear el cliente." : error);
    }

    /// <summary>
    /// Actualiza los datos de un cliente existente.
    /// </summary>
    public async Task<(bool Exito, string? Error)> EditarAsync(Guid id, ClienteGuardarSolicitud solicitud)
    {
        var respuesta = await _http.PutAsJsonAsync($"/api/clientes/{id}", solicitud);

        if (respuesta.IsSuccessStatusCode)
        {
            return (true, null);
        }

        var error = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(error) ? "Error al actualizar el cliente." : error);
    }

    /// <summary>
    /// Solicita el borrado lógico del cliente.
    /// </summary>
    public async Task<bool> BorrarAsync(Guid id)
    {
        var respuesta = await _http.DeleteAsync($"/api/clientes/{id}");
        return respuesta.IsSuccessStatusCode;
    }
}