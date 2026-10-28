namespace NoteWork.Shared.Entidades;

/// <summary>
/// Datos de una partida (línea de presupuesto) para mostrar en pantalla.
/// </summary>
public class PartidaDto
{
    public Guid Id { get; set; }
    public Guid NodoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Especificacion { get; set; }
    public string? Material { get; set; }
    public string? Marca { get; set; }
    public decimal? Cantidad { get; set; }
    public decimal TotalLinea { get; set; }

    // Calculado igual que en la entidad para mostrarlo en la tabla si hay cantidad
    public decimal? PrecioUnitario =>
        Cantidad is > 0 ? Math.Round(TotalLinea / Cantidad.Value, 2) : null;

    public int? AnchoMm { get; set; }
    public int? AltoMm { get; set; }
    public int? ProfundidadMm { get; set; }
    public decimal? Descuento { get; set; }
    public decimal? CostoInterno { get; set; }
    public string? NotaInterna { get; set; }
    public int Orden { get; set; }
}