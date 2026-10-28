using System.Net.Http.Json;
using NoteWork.Shared.Comun;
using NoteWork.Shared.Proyecto;

namespace NoteWork.Client.Servicios;

public class ServicioProyectos
{
    private readonly HttpClient _http;

    public ServicioProyectos(HttpClient http)
    {
        _http = http;
    }

    public async Task<PaginaResultado<ProyectoDto>?> ObtenerProyectosAsync(int pagina = 1, int tamano = 20)
    {
        return await _http.GetFromJsonAsync<PaginaResultado<ProyectoDto>>($"api/proyectos?pagina={pagina}&tamano={tamano}");
    }

    public async Task<ProyectoEditarDto?> ObtenerProyectoPorIdAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<ProyectoEditarDto>($"api/proyectos/{id}");
    }

    public async Task<(bool Exito, string? Error)> CrearProyectoAsync(ProyectoCrearDto dto)
    {
        var respuesta = await _http.PostAsJsonAsync("api/proyectos", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        return (false, "Error al crear el proyecto. Revisa tu conexión.");
    }

    public async Task<(bool Exito, string? Error)> EditarProyectoAsync(Guid id, ProyectoEditarDto dto)
    {
        var respuesta = await _http.PutAsJsonAsync($"api/proyectos/{id}", dto);
        if (respuesta.IsSuccessStatusCode) return (true, null);

        return (false, "Error al editar el proyecto. Revisa tu conexión.");
    }

    public async Task<bool> EliminarProyectoAsync(Guid id)
    {
        var respuesta = await _http.DeleteAsync($"api/proyectos/{id}");
        return respuesta.IsSuccessStatusCode;
    }
}