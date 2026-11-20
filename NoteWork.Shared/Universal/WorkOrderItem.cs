namespace NoteWork.Shared.Entidades.Universal;

/// <summary>
/// Ítem individual dentro de una Orden o Área de Proyecto (tabla WorkOrderItems).
/// Soporta productos 2D (Ventanas, Puertas) y 3D (Gabinetes, Closets) en precisión de 1/16".
/// </summary>
public class WorkOrderItem : EntidadBase
{
    /// <summary>Orden de trabajo / presupuesto al que pertenece este ítem.</summary>
    public Guid WorkOrderId { get; set; }
    public WorkOrder? WorkOrder { get; set; }

    /// <summary>Vínculo opcional a un Nodo (Área: Cocina, Habitación, Baño) de un Proyecto existente.</summary>
    public Guid? AreaNodeId { get; set; }
    public Nodo? AreaNode { get; set; }

    /// <summary>Modelo fabricable seleccionado (ej. Ventana P-65, Gabinete Base Cocina).</summary>
    public Guid ProductModelId { get; set; }
    public ProductModel? ProductModel { get; set; }

    /// <summary>Posición del ítem dentro de la orden o área.</summary>
    public int LineOrder { get; set; }

    /// <summary>Cantidad de unidades iguales con estas mismas medidas.</summary>
    public int Quantity { get; set; } = 1;

    // --- DIMENSIONES EN DIECISEISAVOS DE PULGADA (1/16") ---
    public int WidthSixteenths { get; set; }
    public int HeightSixteenths { get; set; }
    public int? DepthSixteenths { get; set; }

    // --- OPCIONES DINÁMICAS SEGÚN CATEGORÍA ---
    public int LeafCount { get; set; } = 2;
    public int DoorCount { get; set; }
    public int DrawerCount { get; set; }
    public int ShelfCount { get; set; }
    public bool IsLowTrack { get; set; }

    /// <summary>Sobrescritura opcional del grosor del tablero en 1/16" (null = usa el del modelo).</summary>
    public int? BoardThicknessOverrideSixteenths { get; set; }

    /// <summary>Color o acabado elegido para este ítem (ej. "Aluminio Blanco", "Melamina Roble").</summary>
    public string? MaterialColor { get; set; }

    /// <summary>Margen personalizado para este ítem (null = usa el margen por defecto del modelo).</summary>
    public decimal? CustomMarginPercent { get; set; }

    // --- TOTALES CONGELADOS AL GUARDAR ---
    public decimal CalculatedUnitPrice { get; set; }
    public decimal CalculatedLineTotal { get; set; }

    /// <summary>Observaciones específicas de taller o instalación para este ítem.</summary>
    public string? Notes { get; set; }
}