using System.ComponentModel.DataAnnotations;

namespace NoteWork.Shared.Entidades;

/// <summary>
/// Datos necesarios para crear o editar una partida (línea de presupuesto).
/// </summary>
public class PartidaGuardarDto
{
    // [Required] en un Guid: ValidacionFiltro (en la Api) también rechaza Guid.Empty.
    [Required(ErrorMessage = "El área (nodo) es obligatoria.")]
    public Guid NodoId { get; set; }

    [Required(ErrorMessage = "La descripción de la partida es obligatoria.")]
    [MaxLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "La especificación no puede superar los 500 caracteres.")]
    public string? Especificacion { get; set; }

    [MaxLength(150, ErrorMessage = "El material no puede superar los 150 caracteres.")]
    public string? Material { get; set; }

    [MaxLength(150, ErrorMessage = "La marca no puede superar los 150 caracteres.")]
    public string? Marca { get; set; }

    [Range(0.01, 999999999, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public decimal? Cantidad { get; set; }

    [Required(ErrorMessage = "El total de la línea es obligatorio.")]
    [Range(0, 999999999999, ErrorMessage = "El total de la línea no puede ser negativo.")]
    public decimal TotalLinea { get; set; }

    [Range(1, 1000000, ErrorMessage = "El ancho debe ser mayor a cero.")]
    public int? AnchoMm { get; set; }

    [Range(1, 1000000, ErrorMessage = "El alto debe ser mayor a cero.")]
    public int? AltoMm { get; set; }

    [Range(1, 1000000, ErrorMessage = "La profundidad debe ser mayor a cero.")]
    public int? ProfundidadMm { get; set; }

    [Range(0, 999999999999, ErrorMessage = "El descuento no puede ser negativo.")]
    public decimal? Descuento { get; set; }

    [Range(0, 999999999999, ErrorMessage = "El costo interno no puede ser negativo.")]
    public decimal? CostoInterno { get; set; }

    [MaxLength(500, ErrorMessage = "La nota interna no puede superar los 500 caracteres.")]
    public string? NotaInterna { get; set; }

    // CAMBIO (A2, hallazgo 2.1): antes aceptaba cualquier número (negativos o enormes).
    [Range(0, 100000, ErrorMessage = "El orden debe ser un número entre 0 y 100000.")]
    public int Orden { get; set; } = 1;
}