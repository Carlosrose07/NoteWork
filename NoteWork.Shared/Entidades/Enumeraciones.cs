namespace NoteWork.Shared.Entidades;

// Los 4 enums viven juntos en este archivo porque son pequeños y se usan
// siempre en conjunto con las entidades. Cada valor lleva un número explícito.
// Por qué: la base de datos guardará ese número (o su nombre). Si algún día
// reordenamos o insertamos un valor nuevo en medio, los datos ya guardados
// no cambian de significado.

/// <summary>Unidad en la que el usuario ve y escribe las medidas.</summary>
// Ojo: internamente las medidas SIEMPRE se guardan en milímetros (AnchoMm, AltoMm...).
// Este enum solo decide cómo se MUESTRAN y se escriben en pantalla.
public enum UnidadMedida
{
    Pulgadas = 1,
    Centimetros = 2
}

/// <summary>Tipo de proyecto. Decide qué niveles del árbol se usan.</summary>
public enum TipoProyecto
{
    Edificio = 1,       // Puede tener Pisos y Apartamentos
    Casa = 2,           // Sin pisos ni apartamentos: va directo a las Áreas
    TrabajoRapido = 3   // Una sola puerta, un solo closet, etc.
}

/// <summary>Estado del trabajo, del primer presupuesto hasta el cobro.</summary>
public enum EstadoProyecto
{
    Cotizado = 1,
    Aprobado = 2,
    EnProceso = 3,
    Terminado = 4,
    Pagado = 5
}

/// <summary>Nivel que ocupa un Nodo dentro del árbol del presupuesto.</summary>
// Las Partidas (lo que se compra) solo pueden colgar de un nodo de tipo Area.
// Esa regla la validaremos en código más adelante, no aquí.
public enum TipoNodo
{
    Piso = 1,
    Apartamento = 2,
    Area = 3
}