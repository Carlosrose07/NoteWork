using System.Net.Http.Json;
using NoteWork.Shared.Entidades;

namespace NoteWork.Client.Servicios;

public class ServicioNodos
{
    private readonly HttpClient _http;

    public ServicioNodos(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<NodoDto>?> ObtenerNodosPorProyectoAsync(Guid proyectoId)
    {
        return await _http.GetFromJsonAsync<List<NodoDto>>($"api/nodos/proyecto/{proyectoId}");
    }

    public async Task<NodoDto?> ObtenerNodoPorIdAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<NodoDto>($"api/nodos/{id}");
    }

    public async Task<(bool Exito, string? Error)> CrearNodoAsync(NodoGuardarDto dto)
    {
        var respuesta = await _http.PostAsJsonAsync("api/nodos", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var detalle = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(detalle) ? "Error al crear el nodo." : detalle);
    }

    public async Task<(bool Exito, string? Error)> EditarNodoAsync(Guid id, NodoGuardarDto dto)
    {
        var respuesta = await _http.PutAsJsonAsync($"api/nodos/{id}", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var detalle = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(detalle) ? "Error al editar el nodo." : detalle);
    }

    public async Task<(bool Exito, string? Error)> EliminarNodoAsync(Guid id)
    {
        var respuesta = await _http.DeleteAsync($"api/nodos/{id}");
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var detalle = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(detalle) ? "No se pudo eliminar el nodo." : detalle);
    }
}