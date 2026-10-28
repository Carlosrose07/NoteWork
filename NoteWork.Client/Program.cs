using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NoteWork.Client;
using NoteWork.Client.Servicios;     // aquí vive ServicioSesion (4.15.8)
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- DIRECCIÓN DE LA API (4.15.5) ---
// El Client necesita saber dónde vive la Api. La dirección NO va escrita en el
// código: se lee del archivo wwwroot/appsettings.json (clave "ApiUrl").
// Así, al publicar la app, solo se cambia ese archivo y no hay que recompilar.
// (Ojo: este archivo viaja al navegador, así que NUNCA lleva contraseñas ni
// claves; una dirección no es secreta, por eso aquí sí está bien.)
var urlApi = builder.Configuration["ApiUrl"]
    ?? throw new InvalidOperationException(
        "Falta 'ApiUrl' en wwwroot/appsettings.json.");

// El HttpClient apunta a la Api, porque es a quien el Client le habla.
// Un solo HttpClient compartido es liviano: no se crean conexiones nuevas en
// cada pantalla, algo importante en celulares modestos.
// (4.17) El HttpClient ahora pasa por ManejadorToken: cada petición a la Api
// sale con el token en la cabecera Authorization (si hay sesión vigente).
// InnerHandler es el "último eslabón" de la cadena: el que de verdad envía
// la petición por la red. HttpClientHandler en Blazor WASM usa el fetch del
// navegador, así que no agrega peso. Sigue siendo UN solo HttpClient
// compartido (AddScoped), liviano para celulares modestos.
builder.Services.AddScoped(sp =>
{
    var manejador = new ManejadorToken(sp.GetRequiredService<IJSRuntime>())
    {
        InnerHandler = new HttpClientHandler()
    };

    return new HttpClient(manejador) { BaseAddress = new Uri(urlApi) };
});

// --- SERVICIO DE SESIÓN (4.15.8) ---
// "Registrar" el servicio es avisarle a Blazor que existe. Desde ahora, cualquier
// pantalla puede pedirlo y Blazor se lo entrega ya armado (con su HttpClient y
// su IJSRuntime). AddScoped = se crea UNA vez y se comparte mientras la app
// esté abierta, así no se gasta memoria creando copias.
builder.Services.AddScoped<ServicioSesion>();

// --- SERVICIO DE USUARIOS (NUEVO 4.19) ---
// Mismo patrón que ServicioSesion: se crea una vez y se comparte (AddScoped).
// Blazor le entrega solo el HttpClient compartido, que ya trae el token.
builder.Services.AddScoped<ServicioUsuarios>();
builder.Services.AddScoped<ServicioClientes>();
builder.Services.AddScoped<ServicioProyectos>();
builder.Services.AddScoped<ServicioNodos>();
builder.Services.AddScoped<ServicioPartidas>();

// --- AUTENTICACIÓN DE PÁGINAS (4.16) ---
// AddAuthorizationCore activa el sistema de permisos de Blazor ([Authorize]).
// Es la versión "Core": más liviana, suficiente para una app WASM.
builder.Services.AddScoped<ProveedorAutenticacion>();
builder.Services.AddAuthorizationCore();

// Registramos UNA sola instancia de ProveedorAutenticacion (AddScoped) y la
// publicamos también como AuthenticationStateProvider, que es el nombre que
// Blazor busca. Así Blazor y nuestro código comparten el mismo objeto y no
// se crean copias (menos memoria en celulares modestos).
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<ProveedorAutenticacion>());

await builder.Build().RunAsync();