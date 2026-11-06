using System.Net.Http.Json;
using NoteWork.Shared.Comun;
using NoteWork.Shared.Proyecto;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Servicio que comunica las pantallas del Client con los endpoints de proyectos en la Api.
/// Ningún método lanza excepciones: si algo falla, devuelve null/false y deja el mensaje
/// legible en <see cref="UltimoError"/> (hallazgos 3.1, 3.4 y 3.13).
/// </summary>
public class ServicioProyectos
{
    private readonly HttpClient _http;

    public ServicioProyectos(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Mensaje legible del último error ocurrido en este servicio.
    /// Las pantallas lo leen cuando un método devuelve null o false.
    /// Se limpia al inicio de cada llamada.
    /// </summary>
    public string? UltimoError { get; private set; }

    public async Task<PaginaResultado<ProyectoDto>?> ObtenerProyectosAsync(int pagina = 1, int tamano = 20)
    {
        return await LeerAsync<PaginaResultado<ProyectoDto>>(
            $"api/proyectos?pagina={pagina}&tamano={tamano}",
            "No se pudo cargar la lista de proyectos.");
    }

    public async Task<ProyectoEditarDto?> ObtenerProyectoPorIdAsync(Guid id)
    {
        return await LeerAsync<ProyectoEditarDto>(
            $"api/proyectos/{id}",
            "No se pudo cargar el proyecto.");
    }

    public async Task<(bool Exito, string? Error)> CrearProyectoAsync(ProyectoCrearDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsJsonAsync("api/proyectos", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            // Antes siempre decía "Revisa tu conexión", aunque el error fuera otro.
            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo crear el proyecto.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    public async Task<(bool Exito, string? Error)> EditarProyectoAsync(Guid id, ProyectoEditarDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync($"api/proyectos/{id}", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo guardar el proyecto.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    public async Task<bool> EliminarProyectoAsync(Guid id)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.DeleteAsync($"api/proyectos/{id}");
            if (respuesta.IsSuccessStatusCode) return true;

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo eliminar el proyecto.");
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