using System;
using NoteWork.Shared.Entidades;

namespace NoteWork.Shared.Proyecto;

public class ProyectoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid? ClienteId { get; set; }

    public string? ClienteNombre { get; set; }

    public TipoProyecto Tipo { get; set; }
    public EstadoProyecto Estado { get; set; }
    public UnidadMedida UnidadMedida { get; set; }
    public DateTime FechaCreacion { get; set; }
}