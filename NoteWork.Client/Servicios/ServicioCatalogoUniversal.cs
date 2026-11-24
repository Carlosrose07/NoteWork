using System.Net.Http.Json;
using NoteWork.Shared.Dtos;
using NoteWork.Shared.Motor;

namespace NoteWork.Client.Servicios;

/// <summary>
/// Comunica el cliente Blazor WASM con /api/catalogo-universal.
/// Mantiene caché en memoria del paquete completo para que el motor 1/16"
/// y el motor de precios calculen en tiempo real sin viajes extra a la API.
/// </summary>
public class ServicioCatalogoUniversal
{
    private readonly HttpClient _http;

    private CatalogoUniversalPaqueteDto? _cacheActivos;
    private CatalogoUniversalPaqueteDto? _cacheCompleto;

    private IReadOnlyDictionary<Guid, MaterialCatalogEntry>? _materialesMotorCache;
    private IReadOnlyDictionary<Guid, ProductModelDefinition>? _modelosMotorCache;

    public ServicioCatalogoUniversal(HttpClient http)
    {
        _http = http;
    }

    public string? UltimoError { get; private set; }

    /// <summary>
    /// Limpia toda la caché en memoria. Llamar al cerrar sesión o forzar recarga.
    /// </summary>
    public void InvalidarCache()
    {
        _cacheActivos = null;
        _cacheCompleto = null;
        _materialesMotorCache = null;
        _modelosMotorCache = null;
    }

    /// <summary>
    /// Descarga el paquete completo del catálogo en 1 sola llamada HTTP (con caché en memoria).
    /// </summary>
    public async Task<CatalogoUniversalPaqueteDto?> ObtenerPaqueteAsync(
        bool incluirInactivos = false,
        bool forzarRecarga = false)
    {
        if (!forzarRecarga)
        {
            if (!incluirInactivos && _cacheActivos is not null)
                return _cacheActivos;

            if (incluirInactivos && _cacheCompleto is not null)
                return _cacheCompleto;
        }

        var url = incluirInactivos
            ? "/api/catalogo-universal/paquete?incluirInactivos=true"
            : "/api/catalogo-universal/paquete";

        var paquete = await LeerAsync<CatalogoUniversalPaqueteDto>(
            url,
            "No se pudo cargar el catálogo universal.");

        if (paquete is null) return null;

        if (incluirInactivos)
        {
            _cacheCompleto = paquete;
        }
        else
        {
            _cacheActivos = paquete;
            _materialesMotorCache = paquete.Materials.ToDictionary(m => m.Id, m => m.ToMotorEntry());
            _modelosMotorCache = paquete.Models.ToDictionary(m => m.Id, m => m.ToMotorDefinition());
        }

        return paquete;
    }

    /// <summary>
    /// Devuelve los materiales activos en formato del motor (precalculados en memoria).
    /// </summary>
    public IReadOnlyDictionary<Guid, MaterialCatalogEntry> ObtenerMaterialesMotor()
    {
        if (_materialesMotorCache is not null) return _materialesMotorCache;
        if (_cacheActivos is null) return new Dictionary<Guid, MaterialCatalogEntry>();

        _materialesMotorCache = _cacheActivos.Materials.ToDictionary(m => m.Id, m => m.ToMotorEntry());
        return _materialesMotorCache;
    }

    /// <summary>
    /// Devuelve los modelos activos con sus reglas en formato del motor (precalculados en memoria).
    /// </summary>
    public IReadOnlyDictionary<Guid, ProductModelDefinition> ObtenerModelosMotor()
    {
        if (_modelosMotorCache is not null) return _modelosMotorCache;
        if (_cacheActivos is null) return new Dictionary<Guid, ProductModelDefinition>();

        _modelosMotorCache = _cacheActivos.Models.ToDictionary(m => m.Id, m => m.ToMotorDefinition());
        return _modelosMotorCache;
    }

    // =========================================================================
    // CATEGORÍAS (Solo Admin)
    // =========================================================================

    public Task<(ProductCategoryDto? Dato, string? Error)> CrearCategoriaAsync(GuardarProductCategoryDto dto) =>
        EnviarCrearAsync<GuardarProductCategoryDto, ProductCategoryDto>(
            "/api/catalogo-universal/categorias",
            dto,
            "No se pudo crear la categoría.");

    public Task<(bool Exito, string? Error)> EditarCategoriaAsync(Guid id, GuardarProductCategoryDto dto) =>
        EnviarEditarAsync(
            $"/api/catalogo-universal/categorias/{id}",
            dto,
            "No se pudo actualizar la categoría.");

    public Task<(bool Exito, string? Error)> BorrarCategoriaAsync(Guid id) =>
        EnviarBorrarAsync(
            $"/api/catalogo-universal/categorias/{id}",
            "No se pudo eliminar la categoría.");

    // =========================================================================
    // MATERIALES (Solo Admin)
    // =========================================================================

    public Task<(MaterialCatalogDto? Dato, string? Error)> CrearMaterialAsync(GuardarMaterialCatalogDto dto) =>
        EnviarCrearAsync<GuardarMaterialCatalogDto, MaterialCatalogDto>(
            "/api/catalogo-universal/materiales",
            dto,
            "No se pudo crear el material.");

    public Task<(bool Exito, string? Error)> EditarMaterialAsync(Guid id, GuardarMaterialCatalogDto dto) =>
        EnviarEditarAsync(
            $"/api/catalogo-universal/materiales/{id}",
            dto,
            "No se pudo actualizar el material.");

    public Task<(bool Exito, string? Error)> BorrarMaterialAsync(Guid id) =>
        EnviarBorrarAsync(
            $"/api/catalogo-universal/materiales/{id}",
            "No se pudo eliminar el material.");

    // =========================================================================
    // MODELOS FABRICABLES (Solo Admin)
    // =========================================================================

    public Task<(ProductModelDto? Dato, string? Error)> CrearModeloAsync(GuardarProductModelDto dto) =>
        EnviarCrearAsync<GuardarProductModelDto, ProductModelDto>(
            "/api/catalogo-universal/modelos",
            dto,
            "No se pudo crear el modelo.");

    public Task<(bool Exito, string? Error)> EditarModeloAsync(Guid id, GuardarProductModelDto dto) =>
        EnviarEditarAsync(
            $"/api/catalogo-universal/modelos/{id}",
            dto,
            "No se pudo actualizar el modelo.");

    public Task<(bool Exito, string? Error)> BorrarModeloAsync(Guid id) =>
        EnviarBorrarAsync(
            $"/api/catalogo-universal/modelos/{id}",
            "No se pudo eliminar el modelo.");

    public async Task<(bool Exito, string? Error)> DuplicarModeloAsync(Guid id)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsync($"/api/catalogo-universal/modelos/{id}/duplicar", null);
            if (respuesta.IsSuccessStatusCode)
            {
                InvalidarCache();
                return (true, null);
            }

            UltimoError = await ErroresApi.LeerAsync(respuesta, "No se pudo duplicar el modelo.");
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }

    // =========================================================================
    // REGLAS DE CORTE (Solo Admin)
    // =========================================================================

    public Task<(CutRuleDto? Dato, string? Error)> CrearReglaCorteAsync(Guid modeloId, GuardarCutRuleDto dto) =>
        EnviarCrearAsync<GuardarCutRuleDto, CutRuleDto>(
            $"/api/catalogo-universal/modelos/{modeloId}/reglas-corte",
            dto,
            "No se pudo crear la regla de corte.");

    public Task<(bool Exito, string? Error)> EditarReglaCorteAsync(Guid id, GuardarCutRuleDto dto) =>
        EnviarEditarAsync(
            $"/api/catalogo-universal/reglas-corte/{id}",
            dto,
            "No se pudo actualizar la regla de corte.");

    public Task<(bool Exito, string? Error)> BorrarReglaCorteAsync(Guid id) =>
        EnviarBorrarAsync(
            $"/api/catalogo-universal/reglas-corte/{id}",
            "No se pudo eliminar la regla de corte.");

    // =========================================================================
    // REGLAS DE HERRAJES (Solo Admin)
    // =========================================================================

    public Task<(HardwareRuleDto? Dato, string? Error)> CrearReglaHerrajeAsync(Guid modeloId, GuardarHardwareRuleDto dto) =>
        EnviarCrearAsync<GuardarHardwareRuleDto, HardwareRuleDto>(
            $"/api/catalogo-universal/modelos/{modeloId}/reglas-herrajes",
            dto,
            "No se pudo crear la regla de herraje.");

    public Task<(bool Exito, string? Error)> EditarReglaHerrajeAsync(Guid id, GuardarHardwareRuleDto dto) =>
        EnviarEditarAsync(
            $"/api/catalogo-universal/reglas-herrajes/{id}",
            dto,
            "No se pudo actualizar la regla de herraje.");

    public Task<(bool Exito, string? Error)> BorrarReglaHerrajeAsync(Guid id) =>
        EnviarBorrarAsync(
            $"/api/catalogo-universal/reglas-herrajes/{id}",
            "No se pudo eliminar la regla de herraje.");

    // =========================================================================
    // HELPERS HTTP (sin excepciones no controladas + invalidación automática)
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

    private async Task<(TRespuesta? Dato, string? Error)> EnviarCrearAsync<TEnvio, TRespuesta>(
        string url,
        TEnvio dto,
        string mensajePorDefecto) where TRespuesta : class
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PostAsJsonAsync(url, dto);
            if (respuesta.IsSuccessStatusCode)
            {
                InvalidarCache();
                var creado = await respuesta.Content.ReadFromJsonAsync<TRespuesta>();
                return (creado, null);
            }

            UltimoError = await ErroresApi.LeerAsync(respuesta, mensajePorDefecto);
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (null, UltimoError);
    }

    private async Task<(bool Exito, string? Error)> EnviarEditarAsync<TEnvio>(
        string url,
        TEnvio dto,
        string mensajePorDefecto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.PutAsJsonAsync(url, dto);
            if (respuesta.IsSuccessStatusCode)
            {
                InvalidarCache();
                return (true, null);
            }

            UltimoError = await ErroresApi.LeerAsync(respuesta, mensajePorDefecto);
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }

    private async Task<(bool Exito, string? Error)> EnviarBorrarAsync(
        string url,
        string mensajePorDefecto)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.DeleteAsync(url);
            if (respuesta.IsSuccessStatusCode)
            {
                InvalidarCache();
                return (true, null);
            }

            UltimoError = await ErroresApi.LeerAsync(respuesta, mensajePorDefecto);
        }
        catch (Exception ex)
        {
            UltimoError = ErroresApi.DeExcepcion(ex);
        }

        return (false, UltimoError);
    }
}