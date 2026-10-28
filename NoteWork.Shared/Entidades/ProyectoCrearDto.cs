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

    public TipoProyecto Tipo { get; set; } = TipoProyecto.Casa;
    public EstadoProyecto Estado { get; set; } = EstadoProyecto.Cotizado;
    public UnidadMedida UnidadMedida { get; set; } = UnidadMedida.Pulgadas;

    [MaxLength(500, ErrorMessage = "Las notas son muy largas.")]
    public string? Notas { get; set; }
}