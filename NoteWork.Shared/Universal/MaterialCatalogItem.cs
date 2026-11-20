using NoteWork.Shared.Motor;

namespace NoteWork.Shared.Entidades.Universal;

/// <summary>
/// Catálogo maestro de materiales y precios de la empresa (tabla MaterialCatalog).
/// Cubre perfiles/barras 1D, planchas/tableros/cristales 2D y herrajes por unidad.
/// </summary>
public class MaterialCatalogItem : EntidadBase
{
    /// <summary>Código corto único por empresa (ej. "ALU-P65", "MEL-34", "BIS-CAZ").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre descriptivo del material (ej. "Barra Aluminio P-65", "Melamina 3/4 Blanco").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tipo comercial: Linear1D (barras/pies), Sheet2D (planchas/pies²) o Unit (herrajes).</summary>
    public MaterialType MaterialType { get; set; } = MaterialType.Unit;

    /// <summary>Largo comercial en pulgadas para barras/perfiles 1D (ej. 252" = 21 pies). Null si se vende por pie.</summary>
    public decimal? CommercialLengthInches { get; set; }

    /// <summary>Ancho comercial en pulgadas para planchas 2D (ej. 48" tablero o 130" cristal).</summary>
    public decimal? CommercialWidthInches { get; set; }

    /// <summary>Alto comercial en pulgadas para planchas 2D (ej. 96" tablero o 84" cristal).</summary>
    public decimal? CommercialHeightInches { get; set; }

    /// <summary>Costo vigente por unidad comercial (por barra, por plancha, por pie o por unidad).</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Porcentaje de desperdicio estimado al cortar (ej. 10 = 10%).</summary>
    public decimal DefaultWastePercent { get; set; }

    /// <summary>Lista opcional de colores/acabados disponibles separados por coma (ej. "Blanco,Bronce,Negro").</summary>
    public string? ColorOptions { get; set; }

    /// <summary>Indica si el material está disponible para nuevas reglas y cotizaciones.</summary>
    public bool IsActive { get; set; } = true;
}