namespace BaityMu.Domain.Enums
{

    /// <summary>
    /// Productos que Baity puede elaborar con la leche del día: queso o yogurt.
    /// </summary>
    /// <remarks>
    /// ¿Qué es un enum?
    /// Un tipo con una lista cerrada de valores con nombre. Una variable de tipo
    /// TipoProducto solo puede valer Queso o Yogurt; no acepta textos sueltos
    /// como "queso", "Quezo" o "QUESO".
    ///
    /// ¿Para qué lo uso en BaityMu?
    /// - Las reglas del taller dicen que la leche solo se transforma en dos productos
    ///   (RN-02); el enum lo garantiza en todo el código.
    /// - Lo usan los lotes del inventario, las ventas, los parámetros de cada producto
    ///   (rendimiento, vida útil y precio) y los reportes.
    /// - El compilador detecta errores de escritura y Visual Studio autocompleta
    ///   los valores.
    /// - Los números fijos (1 y 2) evitan que cambien si alguien reordena la lista,
    ///   y que el valor por defecto (0) se confunda con un producto real.
    /// </remarks>  
    public enum TipoProducto
    {
        Queso = 1,
        Yogurt = 2
    }
}
