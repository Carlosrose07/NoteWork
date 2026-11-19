using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NoteWork.Shared.Cuenta;      // LoginRespuesta

namespace NoteWork.Client.Servicios;

// ManejadorToken (4.17): se mete en el camino de CADA petición hacia la Api.
// Le pega el token en la cabecera Authorization para que la Api sepa quién eres.
//
// NUEVO (Bloque B): también vigila las RESPUESTAS. Si la Api contesta 401
// (token vencido o usuario desactivado), cierra la sesión y manda a /login
// desde este único lugar. Así todas las pantallas quedan cubiertas.
public class ManejadorToken : DelegatingHandler
{
    // Debe ser el MISMO nombre que usa ServicioSesion.
    private const string ClaveSesion = "notework.sesion";

    private readonly IJSRuntime _js;

    // Se recibe el proveedor de servicios y se pide ServicioSesion SOLO cuando
    // hace falta (en un 401). Si se pidiera al construir el manejador, habría un
    // círculo: ServicioSesion -> HttpClient -> ManejadorToken -> ServicioSesion.
    // Así además se obtiene la MISMA instancia que escucha el menú.
    private readonly IServiceProvider _servicios;

    // Evita cerrar sesión varias veces si llegan varios 401 casi a la vez.
    private bool _cerrandoSesion;

    public ManejadorToken(IJSRuntime js, IServiceProvider servicios)
    {
        _js = js;
        _servicios = servicios;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Solo agregamos el token si la petición no trae uno propio.
        if (request.Headers.Authorization is null)
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", ClaveSesion);

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var sesion = JsonSerializer.Deserialize<LoginRespuesta>(json);

                    // Si el token ya venció no se envía (ahorra un viaje de red).
                    if (sesion is not null
                        && !string.IsNullOrWhiteSpace(sesion.AccessToken)
                        && sesion.ExpiraEn > DateTime.UtcNow)
                    {
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
                    }
                }
                catch (JsonException)
                {
                    // Texto dañado: ServicioSesion lo limpia al leer la sesión.
                }
            }
        }

        var respuesta = await base.SendAsync(request, cancellationToken);

        // NUEVO: un 401 que NO viene del login significa sesión inválida.
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized
            && !EsPeticionDeLogin(request)
            && !_cerrandoSesion)
        {
            await CerrarSesionYVolverAlLoginAsync();
        }

        // La respuesta se devuelve intacta: cada pantalla sigue funcionando igual.
        return respuesta;
    }

    // El login también da 401 con credenciales incorrectas. Eso no es una
    // sesión vencida: no se cierra nada (ServicioSesion ya muestra el mensaje).
    private static bool EsPeticionDeLogin(HttpRequestMessage request)
    {
        var ruta = request.RequestUri?.AbsolutePath ?? string.Empty;
        return ruta.EndsWith("/api/cuenta/login", StringComparison.OrdinalIgnoreCase);
    }

    private async Task CerrarSesionYVolverAlLoginAsync()
    {
        var navegacion = _servicios.GetRequiredService<NavigationManager>();

        // Si ya estamos en /login no se hace nada (evita bucles).
        var rutaActual = navegacion.ToBaseRelativePath(navegacion.Uri);
        if (rutaActual.StartsWith("login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _cerrandoSesion = true;
        try
        {
            // Pasa por ServicioSesion para que se dispare SesionCambio y el
            // menú y la autenticación se enteren.
            var sesion = _servicios.GetRequiredService<ServicioSesion>();
            await sesion.CerrarSesionAsync();

            navegacion.NavigateTo("/login");
        }
        finally
        {
            _cerrandoSesion = false;
        }
    }
}