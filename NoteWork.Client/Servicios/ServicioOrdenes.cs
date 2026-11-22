using System.Net.Http.Json;
using NoteWork.Shared.Dtos;
using NoteWork.Shared.Entidades.Universal;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Comunica Blazor WASM con /api/ordenes (órdenes de trabajo, presupuestos e ítems).
/// Ningún método lanza excepciones: cualquier fallo queda en UltimoError.
/// </summary>
public class ServicioOrdenes
{
    private readonly HttpClient _http;

    public ServicioOrdenes(HttpClient http)
    {
        _http = http;
    }

    public string? UltimoError { get; private set; }

    /// <summary>
    /// Obtiene el listado paginado de órdenes/presupuestos con filtros opcionales.
    /// Si falla, devuelve una página vacía (nunca null) y guarda el motivo en UltimoError.
    /// </summary>
    public async Task<WorkOrdersPaginaDto> ObtenerPaginaAsync(
        int pagina = 1,
        int tamanoPagina = 20,
        string? buscar = null,
        WorkOrderStatus? estado = null,
        WorkOrderType? tipo = null,
        Guid? proyectoId = null)
    {
        var parametros = new List<string>
        {
            $"pagina={Math.Max(1, pagina)}",
            $"tamanoPagina={Math.Clamp(tamanoPagina, 1, 50)}"
        };

        if (!string.IsNullOrWhiteSpace(buscar))
            parametros.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");

        if (estado.HasValue)
            parametros.Add($"estado={estado.Value}");

        if (tipo.HasValue)
            parametros.Add($"tipo={tipo.Value}");

        if (proyectoId.HasValue)
            parametros.Add($"proyectoId={proyectoId.Value}");

        var url = "/api/ordenes?" + string.Join("&", parametros);

        var resultado = await LeerAsync<WorkOrdersPaginaDto>(
            url,
            "No se pudo cargar la lista de órdenes.");

        return resultado ?? new WorkOrdersPaginaDto([], 0, pagina, tamanoPagina);
    }

    /// <summary>
    /// Obtiene una orden completa junto con todos sus ítems.
    /// </summary>
    public Task<WorkOrderDetalleDto?> ObtenerDetalleAsync(Guid id) =>
        LeerAsync<WorkOrderDetalleDto>(
            $"/api/ordenes/{id}",
            "No se pudo cargar el detalle de la orden.");

    /// <summary>
    /// Crea una nueva orden o presupuesto y devuelve su detalle con el número NW-YYYY-XXXX asignado.
    /// </summary>
    public async Task<(WorkOrderDetalleDto? Dato, string? Error)> CrearAsync(CrearActualizarWorkOrderDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsJsonAsync("/api/ordenes", dto);
            if (respuesta.IsSuccessStatusCode)
            {
                var creada = await respuesta.Content.ReadFromJsonAsync<WorkOrderDetalleDto>();
                return (creada, null);
            }

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo crear la orden.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (null, UltimoError);
    }

    /// <summary>
    /// Actualiza cabecera, impuestos, descuentos y totales de una orden existente.
    /// </summary>
    public async Task<(bool Exito, string? Error)> ActualizarAsync(Guid id, CrearActualizarWorkOrderDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync($"/api/ordenes/{id}", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo actualizar la orden.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }

    /// <summary>
    /// Cambia únicamente el estado de la orden (Borrador, Cotizado, En Producción, Completado, etc.).
    /// </summary>
    public async Task<(bool Exito, string? Error)> CambiarEstadoAsync(Guid id, WorkOrderStatus nuevoEstado)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync(
                $"/api/ordenes/{id}/estado",
                new ActualizarEstadoWorkOrderDto(nuevoEstado));

            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo cambiar el estado de la orden.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }

    /// <summary>
    /// Borrado lógico de la orden y todos sus ítems.
    /// </summary>
    public async Task<(bool Exito, string? Error)> BorrarAsync(Guid id)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.DeleteAsync($"/api/ordenes/{id}");
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo eliminar la orden.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }

    // =========================================================================
    // ÍTEMS DE LA ORDEN
    // =========================================================================

    /// <summary>
    /// Agrega un nuevo ítem configurable a la orden.
    /// </summary>
    public async Task<(WorkOrderItemDto? Dato, string? Error)> AgregarItemAsync(
        Guid ordenId,
        CrearActualizarWorkOrderItemDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsJsonAsync($"/api/ordenes/{ordenId}/items", dto);
            if (respuesta.IsSuccessStatusCode)
            {
                var creado = await respuesta.Content.ReadFromJsonAsync<WorkOrderItemDto>();
                return (creado, null);
            }

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo agregar el ítem a la orden.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (null, UltimoError);
    }

    /// <summary>
    /// Actualiza un ítem existente dentro de la orden.
    /// </summary>
    public async Task<(bool Exito, string? Error)> ActualizarItemAsync(
        Guid ordenId,
        Guid itemId,
        CrearActualizarWorkOrderItemDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync($"/api/ordenes/{ordenId}/items/{itemId}", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo actualizar el ítem.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }

    /// <summary>
    /// Elimina (borrado lógico) un ítem de la orden.
    /// </summary>
    public async Task<(bool Exito, string? Error)> BorrarItemAsync(Guid ordenId, Guid itemId)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.DeleteAsync($"/api/ordenes/{ordenId}/items/{itemId}");
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo eliminar el ítem.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }

    // =========================================================================
    // HELPER GET
    // =========================================================================

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