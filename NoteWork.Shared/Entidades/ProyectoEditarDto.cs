using System;
using System.ComponentModel.DataAnnotations;
using NoteWork.Shared.Entidades;

namespace NoteWork.Shared.Proyecto;

public class ProyectoEditarDto
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre no puede pasar de 150 letras.")]
    public string Nombre { get; set; } = string.Empty;

    public Guid? ClienteId { get; set; }

    public TipoProyecto Tipo { get; set; }
    public EstadoProyecto Estado { get; set; }
    public UnidadMedida UnidadMedida { get; set; }

    [MaxLength(500, ErrorMessage = "Las notas son muy largas.")]
    public string? Notas { get; set; }
}