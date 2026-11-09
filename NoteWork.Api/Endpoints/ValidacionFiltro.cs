using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NoteWork.Api.Endpoints;

/// <summary>
/// Filtro de endpoint que EJECUTA las reglas de DataAnnotations de un DTO
/// ([Required], [MaxLength], [StringLength], [Range], [EnumDataType]).
///
/// Por qué existe (hallazgo 2.1): los minimal APIs NO validan los DTO solos.
/// Sin este filtro, las reglas escritas en los DTO eran solo decoración y los
/// datos inválidos llegaban a la base de datos o provocaban errores 500.
///
/// Cómo responde: si algo no es válido, devuelve 400 en formato ValidationProblem
/// ({ "errors": { campo: [mensajes] } }). Ese formato ya lo entiende ErroresApi.cs
/// del Client, así que el usuario verá el mensaje real y NO hay que tocar el Client.
/// </summary>
/// <typeparam name="T">El tipo del DTO que viene en el cuerpo de la petición.</typeparam>
public sealed class ValidacionFiltro<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        // 1) Buscamos, entre los argumentos del endpoint (id, dto, db, user...),
        //    el que es del tipo del DTO.
        T? dto = null;
        foreach (var argumento in context.Arguments)
        {
            if (argumento is T encontrado)
            {
                dto = encontrado;
                break;
            }
        }

        if (dto is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [typeof(T).Name] = new[] { "No se recibieron datos para guardar." }
            });
        }

        // 2) Validamos. Si hay errores, respondemos 400 y el endpoint NI SE EJECUTA.
        var errores = Validar(dto);
        if (errores.Count > 0)
        {
            return Results.ValidationProblem(errores);
        }

        // 3) Todo bien: se ejecuta el endpoint normalmente.
        return await next(context);
    }

    /// <summary>
    /// Devuelve los errores agrupados por campo. Si el diccionario viene vacío, el DTO es válido.
    /// </summary>
    private static Dictionary<string, string[]> Validar(T dto)
    {
        var errores = new Dictionary<string, List<string>>();

        // Agrega un mensaje a un campo sin repetirlo.
        void Agregar(string campo, string mensaje)
        {
            if (!errores.TryGetValue(campo, out var lista))
            {
                lista = new List<string>();
                errores[campo] = lista;
            }

            if (!lista.Contains(mensaje))
            {
                lista.Add(mensaje);
            }
        }

        // --- Reglas de DataAnnotations ---
        // validateAllProperties: true es IMPORTANTE: sin eso solo se revisaría [Required]
        // y se ignorarían [MaxLength], [Range] y [EnumDataType].
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), resultados, validateAllProperties: true);

        foreach (var resultado in resultados)
        {
            var mensaje = resultado.ErrorMessage ?? "Hay un dato que no es válido.";
            var campos = resultado.MemberNames.Any()
                ? resultado.MemberNames
                : new[] { typeof(T).Name };

            foreach (var campo in campos)
            {
                Agregar(campo, mensaje);
            }
        }

        // --- Regla extra: Guid vacío ---
        // [Required] no sirve con un Guid normal: nunca es null, así que
        // Guid.Empty (00000000-0000-...) pasaría como si fuera válido.
        // Por eso, todo Guid marcado con [Required] se rechaza si viene vacío.
        foreach (var propiedad in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (propiedad.PropertyType != typeof(Guid) || !propiedad.CanRead)
            {
                continue;
            }

            var requerido = propiedad.GetCustomAttribute<RequiredAttribute>();
            if (requerido is null)
            {
                continue;
            }

            if ((Guid)propiedad.GetValue(dto)! == Guid.Empty)
            {
                Agregar(propiedad.Name, requerido.ErrorMessage ?? $"{propiedad.Name} es obligatorio.");
            }
        }

        return errores.ToDictionary(par => par.Key, par => par.Value.ToArray());
    }
}

/// <summary>
/// Atajo para activar el filtro en un endpoint. Uso:
///
///   grupo.MapPost("", async (ClienteGuardarSolicitud solicitud, ...) => { ... })
///        .ValidarSolicitud&lt;ClienteGuardarSolicitud&gt;();
///
/// Se agrega al final de cada MapPost y MapPut que reciba un DTO.
/// </summary>
public static class ValidacionExtensiones
{
    public static RouteHandlerBuilder ValidarSolicitud<T>(this RouteHandlerBuilder builder) where T : class
        => builder.AddEndpointFilter<ValidacionFiltro<T>>();
}
