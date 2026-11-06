using System.Net.Http.Json;
using NoteWork.Shared.Entidades;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Servicio que comunica las pantallas del Client con los endpoints de nodos
/// (Pisos, Apartamentos, Áreas) en la Api.
/// Ningún método lanza excepciones: si algo falla, devuelve null/false y deja el mensaje
/// legible en <see cref="UltimoError"/> (hallazgos 3.4 y 3.13).
/// </summary>
public class ServicioNodos
{
    private readonly HttpClient _http;

    public ServicioNodos(HttpClient http)
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
    /// Obtiene el árbol de nodos de un proyecto. Devuelve null si falla.
    /// </summary>
    public async Task<List<NodoDto>?> ObtenerNodosPorProyectoAsync(Guid proyectoId)
    {
        return await LeerAsync<List<NodoDto>>(
            $"api/nodos/proyecto/{proyectoId}",
            "No se pudo cargar la estructura del proyecto.");
    }

    /// <summary>
    /// Obtiene un nodo por su Id. Devuelve null si no existe o si falla.
    /// </summary>
    public async Task<NodoDto?> ObtenerNodoPorIdAsync(Guid id)
    {
        return await LeerAsync<NodoDto>(
            $"api/nodos/{id}",
            "No se pudo cargar el nodo.");
    }

    public async Task<(bool Exito, string? Error)> CrearNodoAsync(NodoGuardarDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsJsonAsync("api/nodos", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            // Antes se mostraba el texto crudo de la Api; ahora pasa por ErroresApi.
            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo crear el nodo.");
        }
        catch (Exception ex)
        {
            // Sin señal, tiempo agotado, etc.: antes esto lanzaba excepción.
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    public async Task<(bool Exito, string? Error)> EditarNodoAsync(Guid id, NodoGuardarDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync($"api/nodos/{id}", dto);
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo editar el nodo.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }
        return (false, UltimoError);
    }

    public async Task<(bool Exito, string? Error)> EliminarNodoAsync(Guid id)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.DeleteAsync($"api/nodos/{id}");
            if (respuesta.IsSuccessStatusCode) return (true, null);

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo eliminar el nodo.");
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