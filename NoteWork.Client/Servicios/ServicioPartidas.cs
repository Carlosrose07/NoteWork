using System.Net.Http.Json;
using NoteWork.Shared.Entidades;

namespace NoteWork.Client.Servicios;

public class ServicioPartidas
{
    private readonly HttpClient _http;

    public ServicioPartidas(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<PartidaDto>?> ObtenerPartidasPorNodoAsync(Guid nodoId)
    {
        return await _http.GetFromJsonAsync<List<PartidaDto>>($"api/partidas/nodo/{nodoId}");
    }

    public async Task<PartidaDto?> ObtenerPartidaPorIdAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<PartidaDto>($"api/partidas/{id}");
    }

    public async Task<(bool Exito, string? Error)> CrearPartidaAsync(PartidaGuardarDto dto)
    {
        var respuesta = await _http.PostAsJsonAsync("api/partidas", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var detalle = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(detalle) ? "Error al crear la partida." : detalle);
    }

    public async Task<(bool Exito, string? Error)> EditarPartidaAsync(Guid id, PartidaGuardarDto dto)
    {
        var respuesta = await _http.PutAsJsonAsync($"api/partidas/{id}", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var detalle = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(detalle) ? "Error al editar la partida." : detalle);
    }

    public async Task<(bool Exito, string? Error)> EliminarPartidaAsync(Guid id)
    {
        var respuesta = await _http.DeleteAsync($"api/partidas/{id}");
        if (respuesta.IsSuccessStatusCode) return (true, null);

        var detalle = await respuesta.Content.ReadAsStringAsync();
        return (false, string.IsNullOrWhiteSpace(detalle) ? "No se pudo eliminar la partida." : detalle);
    }
}