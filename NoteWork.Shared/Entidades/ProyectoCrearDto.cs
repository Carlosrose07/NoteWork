using System;
using System.ComponentModel.DataAnnotations;
using NoteWork.Shared.Entidades;

namespace NoteWork.Shared.Proyecto;

public class ProyectoCrearDto
{
    [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede pasar de 150 letras.")]
    public string Nombre { get; set; } = string.Empty;

    public Guid? ClienteId { get; set; }

    // CAMBIO (A2, hallazgo 2.1): [EnumDataType] rechaza números que no existen en el enum
    // (por ejemplo 99). Sin esto, la Api aceptaba cualquier número y lo guardaba.
    [EnumDataType(typeof(TipoProyecto), ErrorMessage = "El tipo de proyecto no es válido.")]
    public TipoProyecto Tipo { get; set; } = TipoProyecto.Casa;

    [EnumDataType(typeof(EstadoProyecto), ErrorMessage = "El estado del proyecto no es válido.")]
    public EstadoProyecto Estado { get; set; } = EstadoProyecto.Cotizado;

    [EnumDataType(typeof(UnidadMedida), ErrorMessage = "La unidad de medida no es válida.")]
    public UnidadMedida UnidadMedida { get; set; } = UnidadMedida.Pulgadas;

    // NUEVO (D1): descuento general del proyecto (monto fijo o porcentaje).
    [EnumDataType(typeof(TipoDescuento), ErrorMessage = "El tipo de descuento no es válido.")]
    public TipoDescuento TipoDescuento { get; set; } = TipoDescuento.Monto;

    [Range(0, 999999999999, ErrorMessage = "El descuento no puede ser negativo.")]
    public decimal DescuentoValor { get; set; } = 0m;

    [MaxLength(500, ErrorMessage = "Las notas son muy largas.")]
    public string? Notas { get; set; }
}