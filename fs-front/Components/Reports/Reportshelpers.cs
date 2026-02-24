// Reports/ReportsHelpers.cs
// Métodos helper estáticos reutilizados por todos los componentes de Reports

namespace fs_front.Pages.Reports;

public static class ReportsHelpers
{
    /// <summary>Convierte "Marzo 2025" → "Mar 25" para etiquetas de gráficas.</summary>
    public static string ShortenMonth(string month)
    {
        if (string.IsNullOrEmpty(month)) return month;
        var parts = month.Split(' ');
        if (parts.Length == 2 && parts[1].Length == 4)
            return $"{parts[0]} {parts[1][2..]}";
        return month;
    }

    /// <summary>Devuelve clase CSS de color (green/orange/red) según umbrales.</summary>
    public static string PerfColor(double val, double low, double high) =>
        val >= high ? "green" : val >= low ? "orange" : "red";

    /// <summary>Devuelve clase text-* Bootstrap según umbrales.</summary>
    public static string TextColor(double val, double low, double high) =>
        val >= high ? "text-success" : val >= low ? "text-warning" : "text-danger";

    /// <summary>Formatea un decimal como moneda para los formatters de Radzen.</summary>
    public static string FormatCurrency(object value) =>
        value is decimal d ? $"${d:N0}" : value?.ToString() ?? "";
}