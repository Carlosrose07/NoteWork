using NoteWork.Shared.Motor;

namespace NoteWork.Shared.Entidades.Universal;

/// <summary>
/// Modelo fabricable perteneciente a una categoría (ej. "P-65", "GAB-BASE", "CLOSET-MOD").
/// Centraliza la configuración base de grosor, mano de obra y margen para el motor universal.
/// </summary>
public class ProductModel : EntidadBase
{
    /// <summary>Categoría a la que pertenece este modelo (Ventanas, Puertas, Gabinetes, Closets).</summary>
    public Guid CategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    /// <summary>Código corto único por empresa (ej. "P-65", "GAB-BASE").</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre visible del modelo (ej. "Ventana P-65", "Gabinete Base Cocina").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Grosor estándar del tablero en dieciseisavos de pulgada (ej. melamina 3/4" = 12).
    /// En productos de aluminio/cristal que no usan tablero, se deja en 0.
    /// </summary>
    public int DefaultBoardThicknessSixteenths { get; set; }

    /// <summary>Modo por defecto para calcular la mano de obra (Fixed, PerSqFt o Percent).</summary>
    public LaborMode DefaultLaborMode { get; set; } = LaborMode.Percent;

    /// <summary>Valor base de mano de obra según el modo configurado.</summary>
    public decimal DefaultLaborValue { get; set; }

    /// <summary>Porcentaje de margen de ganancia por defecto (ej. 30 = 30%).</summary>
    public decimal DefaultMarginPercent { get; set; }

    /// <summary>Orden de aparición dentro de su categoría.</summary>
    public int SortOrder { get; set; }

    /// <summary>Indica si el modelo está activo para nuevas órdenes y presupuestos.</summary>
    public bool IsActive { get; set; } = true;
}