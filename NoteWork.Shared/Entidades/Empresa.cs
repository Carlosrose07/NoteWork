namespace NoteWork.Shared.Entidades;

/// <summary>
/// La empresa que usa NoteWork (hoy la de tu hermano; mañana, otras).
/// Hereda de EntidadBase, así que ya trae Id, EmpresaId, FechaCreacion,
/// FechaModificacion y Eliminado sin que los escribamos otra vez.
/// </summary>
public class Empresa : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;

    // Actividad o eslogan breve debajo del nombre en el presupuesto A4
    public string? Subtitulo { get; set; }

    // RNC o identificación fiscal (opcional en el encabezado A4)
    public string? Rnc { get; set; }

    // Ruta URL o DataURL ligero del logo (opcional)
    public string? LogoRuta { get; set; }

    // Opcionales: el PDF los mostrará si existen
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }

    // Código de la moneda (DOP = peso dominicano)
    public string Moneda { get; set; } = "DOP";

    // Unidad por defecto al crear proyectos nuevos
    public UnidadMedida UnidadPorDefecto { get; set; } = UnidadMedida.Pulgadas;

    // Límite de usuarios permitidos para esta empresa (solo SuperAdmin lo modifica)
    public int MaxUsuarios { get; set; } = 3;

    // Permite pausar el acceso de la empresa si vence la licencia
    public bool Activa { get; set; } = true;
}