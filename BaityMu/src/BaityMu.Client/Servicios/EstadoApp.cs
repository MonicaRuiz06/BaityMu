using BaityMu.Client.Modelos;

namespace BaityMu.Client.Servicios;

/// <summary>
/// Estado compartido por todas las pantallas:
/// - el **día de trabajo**: la fecha que usan por defecto los formularios y consultas.
///   Sirve para recorrer el escenario del taller (2026-12-01, 02, 03, 17...) sin esperar
///   a que pasen los días de verdad;
/// - los **avisos** (mensajes que aparecen abajo a la derecha y se van solos).
/// </summary>
public sealed class EstadoApp
{
    private DateOnly _dia = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Se dispara cuando cambia el día de trabajo, para que las pantallas recarguen.</summary>
    public event Action? DiaCambiado;

    /// <summary>Se dispara cuando llega o se va un aviso.</summary>
    public event Action? AvisosCambiaron;

    public DateOnly Dia
    {
        get => _dia;
        set
        {
            if (_dia == value) return;
            _dia = value;
            DiaCambiado?.Invoke();
        }
    }

    public List<Aviso> Avisos { get; } = new();

    public void Exito(string mensaje) => Agregar(new Aviso(TipoAviso.Exito, mensaje));

    public void Error(string mensaje) => Agregar(new Aviso(TipoAviso.Error, mensaje));

    /// <summary>Muestra el error de la API con su título y su detalle.</summary>
    public void Error(ApiException e) => Agregar(new Aviso(TipoAviso.Error, e.Detalle, e.Titulo));

    public void Cerrar(Aviso aviso)
    {
        if (Avisos.Remove(aviso))
            AvisosCambiaron?.Invoke();
    }

    private async void Agregar(Aviso aviso)
    {
        Avisos.Add(aviso);
        AvisosCambiaron?.Invoke();
        await Task.Delay(aviso.Tipo == TipoAviso.Error ? 7000 : 4000);
        Cerrar(aviso);
    }
}

public enum TipoAviso { Exito, Error }

public sealed record Aviso(TipoAviso Tipo, string Mensaje, string? Titulo = null);

/// <summary>Formatos de número y fecha en español para toda la app.</summary>
public static class Formato
{
    private static readonly System.Globalization.CultureInfo Es = new("es-CO");

    /// <summary>15 → "15", 14.5 → "14,5", 3.333 → "3,333".</summary>
    public static string Cantidad(decimal valor) => valor.ToString("#,0.###", Es);

    public static string Litros(decimal valor) => $"{Cantidad(valor)} L";

    public static string Pesos(decimal valor) => valor.ToString("$ #,0", Es);

    /// <summary>"1 dic 2026", con espacios que no se parten en dos líneas.</summary>
    public static string Fecha(DateOnly fecha) => fecha.ToString("d MMM yyyy", Es).Replace(' ', (char)160); // 160 = espacio que no se parte

    public static string FechaLarga(DateOnly fecha) => fecha.ToString("dddd d 'de' MMMM 'de' yyyy", Es);

    public static string Unidad(TipoProducto tipo) => tipo == TipoProducto.Queso ? "kg" : "L";

    public static string ConUnidad(decimal valor, TipoProducto tipo) => $"{Cantidad(valor)} {Unidad(tipo)}";
}
