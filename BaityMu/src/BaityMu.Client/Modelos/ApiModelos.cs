// Modelos del front: la misma forma que los DTOs que devuelve la API.
//
// ¿Por qué no se referencia BaityMu.Services para reutilizar sus DTOs?
// Porque el front corre en el navegador y solo debe conocer el "contrato" HTTP de la API,
// no sus capas internas. Así el front y la API se pueden cambiar por separado.

namespace BaityMu.Client.Modelos;

/// <summary>Productos que se elaboran con la leche.</summary>
public enum TipoProducto
{
    Queso = 1,
    Yogurt = 2
}

/// <summary>Estado de un lote en una fecha.</summary>
public enum EstadoLote
{
    Disponible = 1,
    Agotado = 2,
    Vencido = 3,
    Dañado = 4
}

// ---------------------------------------------------------------- Granja (HU-05, HU-06)

public sealed class Granja
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int NumeroVacas { get; set; }
    public decimal LitrosPorVaca { get; set; }
    public decimal ProduccionDiariaEsperada { get; set; }
    public List<ParametroProducto> Productos { get; set; } = new();
}

public sealed class ParametroProducto
{
    public TipoProducto TipoProducto { get; set; }
    public string Unidad { get; set; } = "";
    public decimal LitrosPorUnidad { get; set; }
    public int VidaUtilDias { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public sealed class ActualizarGranja
{
    public decimal? LitrosPorVaca { get; set; }
    public List<ActualizarProducto> Productos { get; set; } = new();
}

public sealed class ActualizarProducto
{
    public TipoProducto TipoProducto { get; set; }
    public string? Unidad { get; set; }
    public decimal LitrosPorUnidad { get; set; }
    public int VidaUtilDias { get; set; }
    public decimal PrecioUnitario { get; set; }
}

// ---------------------------------------------------------------- Vacas (HU-01 a HU-04)

public class Vaca
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Raza { get; set; } = "";
    public bool Activa { get; set; }
    public DateOnly FechaIngreso { get; set; }
    public decimal LitrosProducidos { get; set; }
}

public sealed class VacaDetalle : Vaca
{
    public List<ProduccionDeUnDia> Historial { get; set; } = new();
}

public sealed class ProduccionDeUnDia
{
    public DateOnly Fecha { get; set; }
    public decimal Litros { get; set; }
}

public sealed class CrearVaca
{
    public string Raza { get; set; } = "";
    public string? Nombre { get; set; }
    public DateOnly? FechaIngreso { get; set; }
}

// ---------------------------------------------------------------- Producción (HU-07, HU-08)

public sealed class ProduccionDiaria
{
    public DateOnly Fecha { get; set; }
    public decimal TotalLitros { get; set; }
    public decimal LitrosTransformados { get; set; }
    public decimal LitrosDisponibles { get; set; }
    public List<ProduccionVacaDia> Vacas { get; set; } = new();
}

public sealed class ProduccionVacaDia
{
    public int VacaId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal Litros { get; set; }
}

public sealed class RegistrarProduccion
{
    public DateOnly? Fecha { get; set; }
    public List<ProduccionVaca> Vacas { get; set; } = new();
}

public sealed class ProduccionVaca
{
    public int VacaId { get; set; }
    public decimal Litros { get; set; }
}

// ---------------------------------------------------------------- Transformación e inventario (HU-09, HU-10)

public sealed class TransformarLeche
{
    public DateOnly? Fecha { get; set; }
    public TipoProducto TipoProducto { get; set; } = TipoProducto.Queso;
    public decimal Litros { get; set; }
}

public sealed class Lote
{
    public int Id { get; set; }
    public TipoProducto TipoProducto { get; set; }
    public DateOnly FechaProduccion { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal LitrosUsados { get; set; }
    public decimal CantidadInicial { get; set; }
    public decimal CantidadDisponible { get; set; }
    public decimal CantidadDanada { get; set; }
    public EstadoLote Estado { get; set; }
}

public sealed class ResultadoVencimientos
{
    public DateOnly Fecha { get; set; }
    public List<LoteDañado> Lotes { get; set; } = new();
    public List<TotalDañado> TotalesPorProducto { get; set; } = new();
}

public sealed class LoteDañado
{
    public int LoteId { get; set; }
    public TipoProducto TipoProducto { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal CantidadDañada { get; set; }
}

public sealed class TotalDañado
{
    public TipoProducto TipoProducto { get; set; }
    public decimal CantidadDañada { get; set; }
}

public sealed class ResumenInventario
{
    public TipoProducto TipoProducto { get; set; }
    public decimal CantidadVendible { get; set; }
    public decimal CantidadVencidaSinProcesar { get; set; }
    public decimal CantidadDañada { get; set; }
}
