// Reports/ReportsModels.cs
// Modelos locales compartidos entre los componentes de Reports

namespace fs_front.Pages.Reports;

public class ShortTrendItem
{
    public string ShortMonth { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class FinSummaryItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}










