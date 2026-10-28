namespace NoteWork.Shared.Entidades;

/// <summary>
/// Un presupuesto completo (la "hoja" del cuaderno). Es la raíz del árbol:
/// de aquí cuelgan los Nodos (pisos, apartamentos, áreas) y, dentro de las
/// áreas, las Partidas. Hereda de EntidadBase.
/// </summary>
public class Proyecto : EntidadBase
{
    // Cliente al que pertenece el presupuesto.
    // Es opcional (Guid? con "?"): en un trabajo rápido se puede crear el
    // presupuesto primero y asignar el cliente después, sin frenar al usuario.
    public Guid? ClienteId { get; set; }

    // Nombre con el que el usuario reconoce el trabajo
    // (ej. "Torre Los Álamos" o "Closet de María").
    public string Nombre { get; set; } = string.Empty;

    // Decide qué niveles del árbol se usan:
    // Edificio = pisos y apartamentos, Casa = directo a áreas,
    // TrabajoRapido = una sola puerta o closet.
    public TipoProyecto Tipo { get; set; } = TipoProyecto.Casa;

    // Todo proyecto nuevo empieza como "Cotizado".
    public EstadoProyecto Estado { get; set; } = EstadoProyecto.Cotizado;

    // Unidad con la que se MUESTRAN y escriben las medidas en este proyecto.
    // Internamente las medidas siempre van en milímetros (en la Partida);
    // esto solo cambia la presentación en pantalla.
    public UnidadMedida UnidadMedida { get; set; } = UnidadMedida.Pulgadas;

    // Hasta qué fecha vale la cotización (ej. 15 días desde hoy).
    // Es opcional: puede que un presupuesto no tenga vencimiento.
    // Va en UTC igual que las demás fechas, por el requisito de PostgreSQL.
    public DateTime? FechaValidez { get; set; }

    // Notas internas del proyecto. No salen en el PDF.
    public string? Notas { get; set; }

    // --- Relaciones (las usa Entity Framework para entender la estructura) ---

    // El objeto Cliente completo. Es opcional y NO se carga solo: hay que
    // pedirlo expresamente. Así, al listar proyectos en un celular modesto,
    // no se descargan datos de clientes que nadie va a ver en esa pantalla.
    public Cliente? Cliente { get; set; }

    // Todos los nodos del árbol de este proyecto. Se guarda como una lista
    // PLANA (cada nodo apunta a su padre con PadreId), no como un árbol anidado.
    // Por qué: una sola consulta trae todo el proyecto y el armado del árbol
    // se hace después en memoria. Es mucho más liviano que una consulta por nivel.
    public List<Nodo> Nodos { get; set; } = new();
}