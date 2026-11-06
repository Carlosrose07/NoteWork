using System.Net.Http.Json;
using NoteWork.Shared.Entidades;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Servicio que comunica las pantallas del Client con los endpoints de partidas en la Api.
/// Ningún método lanza excepciones: si algo falla, devuelve null/false y deja el mensaje
/// legible en <see cref="UltimoError"/> (hallazgos 3.4 y 3.13).
/// </summary>
public class ServicioPartidas
{
    private readonly HttpClient _http;

    public ServicioPartidas(HttpClient http)
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
    /// Obtiene las partidas de un nodo (Área). Devuelve null si falla.
    /// </summary>
    public async Task<List<PartidaDto>?> ObtenerPartidasPorNodoAsync(Guid nodoId)
    {
        return await LeerAsync<List<PartidaDto>>(
            $"api/partidas/nodo/{nodoId}",
            "No se pudieron cargar las partidas.");
    }

    /// <summary>
    /// Obtiene una partida por su Id. Devuelve null si no existe o si falla.
    /// </summary>
    public async Task<PartidaDto?> ObtenerPartidaPorIdAsync(Guid id)
    {
        return await LeerAsync<PartidaDto>(
            $"api/partidas/{id}",
            "No se pudo cargar la partida.");
    }

    public async Task<(bool Exito, string? Error)> CrearPartidaAsync(PartidaGuardarDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsJsonAsync("api/partidas", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            // Antes se mostraba el texto crudo de la Api; ahora pasa por ErroresApi.
            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo crear la partida.");
        }
        catch (Exception ex)
        {
            // Sin señal, tiempo agotado, etc.: antes esto lanzaba excepción.
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    public async Task<(bool Exito, string? Error)> EditarPartidaAsync(Guid id, PartidaGuardarDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync($"api/partidas/{id}", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo editar la partida.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    public async Task<(bool Exito, string? Error)> EliminarPartidaAsync(Guid id)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.DeleteAsync($"api/partidas/{id}");
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo eliminar la partida.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
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