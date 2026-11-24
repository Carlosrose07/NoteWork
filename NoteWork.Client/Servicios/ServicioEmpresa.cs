using System.Net.Http.Json;
using NoteWork.Shared.Dtos;

namespace NoteWork.Client.Servicios;

public class ServicioEmpresa
{
    private readonly HttpClient _http;
    private EmpresaDto? _cache;

    public ServicioEmpresa(HttpClient http)
    {
        _http = http;
    }

    public string? UltimoError { get; private set; }

    public void InvalidarCache() => _cache = null;

    public async Task<EmpresaDto?> ObtenerAsync(bool forzarRecarga = false)
    {
        if (!forzarRecarga && _cache is not null)
            return _cache;

        UltimoError = null;
        try
        {
            var respuesta = await _http.GetAsync("/api/empresa");
            if (!respuesta.IsSuccessStatusCode)
            {
                UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudieron cargar los datos de la empresa.");
                return null;
            }

            _cache = await respuesta.Content.ReadFromJsonAsync<EmpresaDto>();
            return _cache;
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
            return null;
        }
    }

    public async Task<(EmpresaDto? Dato, string? Error)> GuardarAsync(GuardarEmpresaDto dto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync("/api/empresa", dto);
            if (respuesta.IsSuccessStatusCode)
            {
                var actualizada = await respuesta.Content.ReadFromJsonAsync<EmpresaDto>();
                _cache = actualizada;
                return (actualizada, null);
            }

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudieron guardar los datos de la empresa.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (null, UltimoError);
    }
}