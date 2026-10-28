namespace NoteWork.Shared.Entidades;

/// <summary>
/// La empresa que usa NoteWork (hoy la de tu hermano; mañana, otras).
/// Hereda de EntidadBase, así que ya trae Id, EmpresaId, FechaCreacion,
/// FechaModificacion y Eliminado sin que los escribamos otra vez.
/// </summary>
public class Empresa : EntidadBase
{
    // Nombre que aparecerá en el encabezado del PDF de la cotización.
    // "= string.Empty" evita valores nulos: un texto vacío es más seguro
    // que un null, que provoca errores si lo usamos sin revisar.
    public string Nombre { get; set; } = string.Empty;

    // Se guarda la RUTA o dirección del archivo del logo, NO la imagen dentro
    // de la base de datos. Por qué: las imágenes dentro de la base la vuelven
    // pesada y lenta; es mejor guardar el archivo aparte y aquí solo su ubicación.
    // El "?" significa que es opcional (puede no tener logo todavía).
    public string? LogoRuta { get; set; }

    // Opcionales: el PDF los mostrará si existen.
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }

    // Código de la moneda (DOP = peso dominicano). Se guarda como texto corto
    // y no como símbolo ("RD$") porque el código es estándar y no cambia.
    // Si otra empresa usa dólares, solo cambia este valor a "USD".
    public string Moneda { get; set; } = "DOP";

    // Unidad con la que se crean los proyectos nuevos de esta empresa.
    // Cada proyecto (y cada usuario) puede elegir otra, según el documento.
    // Pulgadas por defecto, porque es lo que usa el cuaderno del hermano.
    public UnidadMedida UnidadPorDefecto { get; set; } = UnidadMedida.Pulgadas;

    // NOTA: como Empresa hereda EmpresaId de EntidadBase, en una empresa ese
    // campo contendrá su propio Id (EmpresaId = Id). Lo haremos al crearla.
    // Así TODAS las tablas, incluida esta, se filtran de la misma forma.
}