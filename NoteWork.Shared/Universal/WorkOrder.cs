namespace NoteWork.Shared.Entidades.Universal;

/// <summary>Modo de creación de la orden/presupuesto.</summary>
public enum WorkOrderType
{
    /// <summary>Orden rápida directa desde Inicio (sin estructura de pisos/áreas).</summary>
    QuickOrder = 1,
    /// <summary>Orden vinculada a un Proyecto y sus Áreas existentes en NoteWork.</summary>
    ProjectLinked = 2
}

/// <summary>Estado operativo y comercial de la orden.</summary>
public enum WorkOrderStatus
{
    Draft = 1,
    Quoted = 2,
    InProduction = 3,
    Completed = 4
}

/// <summary>
/// Cabecera unificada de Orden de Producción y Presupuesto (tabla WorkOrders).
/// Hereda de EntidadBase (Id, EmpresaId, FechaCreacion, FechaModificacion, Eliminado).
/// </summary>
public class WorkOrder : EntidadBase
{
    /// <summary>Dueño de la orden (regla multi-usuario: usuario normal ve lo suyo; Admin ve toda su empresa).</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Número legible de orden/cotización (ej. "NW-2026-0001").</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Indica si es Orden Rápida o vinculada a un Proyecto.</summary>
    public WorkOrderType OrderType { get; set; } = WorkOrderType.QuickOrder;

    /// <summary>Vínculo opcional a un Cliente registrado en NoteWork.</summary>
    public Guid? CustomerId { get; set; }
    public Cliente? Customer { get; set; }

    /// <summary>Vínculo opcional a un Proyecto existente en NoteWork.</summary>
    public Guid? ProjectId { get; set; }
    public Proyecto? Project { get; set; }

    /// <summary>Datos del cliente guardados en la orden (funcionan incluso sin cliente registrado).</summary>
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }

    /// <summary>Estado actual de la orden.</summary>
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

    /// <summary>Porcentaje de ITBIS aplicado (ej. 18 = 18%, 0 = sin impuesto).</summary>
    public decimal TaxRatePercent { get; set; }

    /// <summary>Porcentaje de descuento aplicado sobre el subtotal del presupuesto.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Totales históricos calculados y congelados al guardar/cotizar.</summary>
    public decimal SubtotalMaterials { get; set; }
    public decimal SubtotalLabor { get; set; }
    public decimal TotalQuoteAmount { get; set; }

    /// <summary>Notas generales o condiciones de validez visibles en el PDF de cotización.</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Instantánea opcional en JSON de los precios vigentes al emitir la cotización,
    /// para que cambios futuros en el catálogo no alteren presupuestos ya entregados.
    /// </summary>
    public string? PricingSnapshotJson { get; set; }
}