using NoteWork.Shared.Motor;

namespace NoteWork.Shared.Entidades.Universal;

/// <summary>
/// Regla declarativa de herrajes y accesorios (tabla ProductHardwareRules).
/// Calcula automáticamente cantidades de bisagras, ruedas, correderas, goma, tiradores, etc.
/// </summary>
public class ProductHardwareRule : EntidadBase
{
    /// <summary>Modelo fabricable al que pertenece esta regla.</summary>
    public Guid ProductModelId { get; set; }
    public ProductModel? ProductModel { get; set; }

    /// <summary>Material/herraje del catálogo que se asigna con esta regla.</summary>
    public Guid MaterialId { get; set; }
    public MaterialCatalogItem? Material { get; set; }

    /// <summary>Base de cálculo: por unidad, por hoja, por puerta, por gaveta, por repisa, perímetro o área.</summary>
    public HardwareCalculationBasis CalculationBasis { get; set; } = HardwareCalculationBasis.PerUnit;

    /// <summary>Multiplicador aplicado a la base (ej. 2 ruedas por hoja, 2 bisagras por puerta).</summary>
    public decimal Factor { get; set; } = 1m;

    /// <summary>
    /// Condición opcional en formato JSON (serialización de RuleCondition) para activar
    /// la regla solo en casos específicos (ej. Riel Bajo o 3/4 hojas). Null = aplica siempre.
    /// </summary>
    public string? ConditionJson { get; set; }

    /// <summary>Orden de aparición en el consolidado de herrajes.</summary>
    public int SortOrder { get; set; }
}