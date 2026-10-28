namespace NoteWork.Shared.Entidades;

/// <summary>
/// Un nivel del presupuesto: Piso, Apartamento o Área.
/// Todos los niveles viven en UNA sola tabla y se relacionan por PadreId,
/// formando un árbol. Por qué una tabla y no tres: los niveles son opcionales
/// (una casa no tiene pisos) y así el mismo código sirve para un edificio,
/// una casa o un trabajo rápido, sin duplicar lógica.
/// </summary>
public class Nodo : EntidadBase
{
    // Proyecto al que pertenece este nodo. Es obligatorio (no lleva "?"):
    // un nodo nunca existe suelto. Guardarlo en CADA nodo permite traer
    // todo el árbol de un proyecto con una sola consulta, algo muy
    // importante para que un celular modesto cargue rápido.
    public Guid ProyectoId { get; set; }

    // El nodo "padre". Es nulo cuando el nodo cuelga directamente del proyecto
    // (ej. el primer Piso, o un Área de una casa). Si tiene valor, apunta al
    // nodo de arriba (ej. un Apartamento apunta a su Piso).
    public Guid? PadreId { get; set; }

    // Qué nivel es: Piso, Apartamento o Area.
    // Regla (la validaremos en código más adelante, no aquí): las Partidas
    // solo pueden colgar de nodos de tipo Area.
    public TipoNodo Tipo { get; set; } = TipoNodo.Area;

    // Nombre que ve el usuario ("Piso 2", "Apto 2B", "Cocina").
    public string Nombre { get; set; } = string.Empty;

    // Posición entre sus hermanos (0, 1, 2...). Se guarda un número porque
    // las bases de datos no garantizan ningún orden por sí solas. Gracias a
    // esto el usuario podrá reordenar y la lista saldrá igual siempre.
    public int Orden { get; set; }

    // --- Relaciones (las usa Entity Framework para entender la estructura) ---

    // El proyecto completo. No se carga solo: hay que pedirlo expresamente.
    public Proyecto? Proyecto { get; set; }

    // El nodo padre. Tampoco se carga solo.
    public Nodo? Padre { get; set; }

    // Los nodos hijos (ej. los Apartamentos de un Piso). Esta lista permite
    // a EF Core entender la relación de árbol. Se inicia vacía para evitar
    // errores de valor nulo.
    public List<Nodo> Hijos { get; set; } = new();

    // Las partidas (lo que se compra) de este nodo. Solo tendrá contenido
    // si el nodo es de tipo Area.
    public List<Partida> Partidas { get; set; } = new();
}