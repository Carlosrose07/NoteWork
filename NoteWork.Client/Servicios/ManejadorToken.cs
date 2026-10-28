using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.JSInterop;
using NoteWork.Shared.Cuenta;      // LoginRespuesta

namespace NoteWork.Client.Servicios;

// ManejadorToken (4.17): un "manejador" se mete en el camino de CADA petición
// que sale hacia la Api. Aquí le pegamos el token en la cabecera Authorization
// para que la Api sepa quién eres. Así ninguna pantalla tiene que acordarse de
// enviarlo, y no hay que copiar este código en todos lados.
public class ManejadorToken : DelegatingHandler
{
    // Debe ser el MISMO nombre que usa ServicioSesion para guardar la sesión.
    private const string ClaveSesion = "notework.sesion";

    private readonly IJSRuntime _js;

    public ManejadorToken(IJSRuntime js)
    {
        _js = js;
    }

    // Blazor llama a este método justo antes de enviar cada petición.
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

                    // Si el token ya venció no se envía: la Api lo rechazaría
                    // de todos modos y así nos ahorramos un viaje de red.
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
                    // Texto dañado: se ignora aquí. ServicioSesion lo limpia
                    // la próxima vez que lea la sesión.
                }
            }
        }

        // Continúa el camino normal: envía la petición a la Api.
        return await base.SendAsync(request, cancellationToken);
    }
}