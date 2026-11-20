namespace NoteWork.Shared.Entidades.Universal;

/// <summary>
/// Categoría de fabricación (Ventanas, Puertas, Gabinetes, Closets, etc.).
/// Sus banderas controlan qué campos muestra dinámicamente el formulario de captura.
/// </summary>
public class ProductCategory : EntidadBase
{
    /// <summary>Código corto único por empresa (ej. "WINDOW", "DOOR", "CABINET", "CLOSET").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre visible en la interfaz (ej. "Ventanas", "Gabinetes").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Activa el campo de Profundidad (3D: Gabinetes y Closets).</summary>
    public bool UsesDepth { get; set; }

    /// <summary>Activa el campo de Cantidad de Hojas (Ventanas y Puertas corredizas).</summary>
    public bool UsesLeaves { get; set; }

    /// <summary>Activa el campo de Cantidad de Puertas (Gabinetes y Closets).</summary>
    public bool UsesDoors { get; set; }

    /// <summary>Activa el campo de Cantidad de Gavetas (Gabinetes y Closets).</summary>
    public bool UsesDrawers { get; set; }

    /// <summary>Activa el campo de Cantidad de Tramos / Repisas (Gabinetes y Closets).</summary>
    public bool UsesShelves { get; set; }

    /// <summary>Activa la opción de Riel Bajo (Ventanas y Puertas corredizas).</summary>
    public bool UsesLowTrack { get; set; }

    /// <summary>Unidad por defecto (ej. "in" para pulgadas fraccionarias).</summary>
    public string DefaultUnit { get; set; } = "in";

    /// <summary>Orden de aparición en pestañas y tarjetas de Inicio.</summary>
    public int SortOrder { get; set; }
}