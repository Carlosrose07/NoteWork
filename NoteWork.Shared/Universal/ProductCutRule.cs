using NoteWork.Shared.Motor;

namespace NoteWork.Shared.Entidades.Universal;

/// <summary>
/// Regla declarativa de corte (tabla ProductCutRules) para cualquier producto 2D o 3D.
/// Define cómo calcular las medidas en 1/16" y la cantidad de piezas a cortar para una parte del modelo.
/// </summary>
public class ProductCutRule : EntidadBase
{
    /// <summary>Modelo fabricable al que pertenece esta regla.</summary>
    public Guid ProductModelId { get; set; }
    public ProductModel? ProductModel { get; set; }

    /// <summary>Nombre de la pieza en la hoja de taller (ej. "Llavín", "Costado", "Puerta", "Cristal").</summary>
    public string PartName { get; set; } = string.Empty;

    /// <summary>Material del catálogo que se descuenta/compra con esta pieza.</summary>
    public Guid MaterialId { get; set; }
    public MaterialCatalogItem? Material { get; set; }

    /// <summary>Cómo se mide la pieza: Length1D (lineal), Rect2D (área) o Perimeter1D (perímetro).</summary>
    public MeasureMode MeasureMode { get; set; } = MeasureMode.Length1D;

    // --- DIMENSIÓN 1 (Largo en 1D, o Ancho en 2D/Perímetro) ---
    public DimensionAxis Dim1Axis { get; set; } = DimensionAxis.Width;
    public DivisorSource Dim1DivisorSource { get; set; } = DivisorSource.ConstantOne;
    public decimal Dim1CustomDivisor { get; set; } = 1m;
    public int Dim1OffsetSixteenths { get; set; }
    public int Dim1ThicknessMultiplier { get; set; }

    // --- DIMENSIÓN 2 (Solo usada en Rect2D y Perimeter1D) ---
    public DimensionAxis Dim2Axis { get; set; } = DimensionAxis.None;
    public DivisorSource Dim2DivisorSource { get; set; } = DivisorSource.ConstantOne;
    public decimal Dim2CustomDivisor { get; set; } = 1m;
    public int Dim2OffsetSixteenths { get; set; }
    public int Dim2ThicknessMultiplier { get; set; }

    // --- CANTIDAD DE PIEZAS ---
    public QuantityMode QuantityMode { get; set; } = QuantityMode.Fixed;
    public int QuantityFactor { get; set; } = 1;

    /// <summary>
    /// Condición opcional en formato JSON (serialización de RuleCondition) para activar
    /// la regla solo en ciertos casos (ej. Riel Bajo o 3/4 hojas). Null = aplica siempre.
    /// </summary>
    public string? ConditionJson { get; set; }

    /// <summary>Orden en que aparece la pieza en la Hoja de Producción.</summary>
    public int SortOrder { get; set; }
}