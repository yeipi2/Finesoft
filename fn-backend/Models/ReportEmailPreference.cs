// Models/ReportEmailPreference.cs
using System.ComponentModel.DataAnnotations;

namespace fn_backend.Models;

public class ReportEmailPreference
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public bool AutoSendEnabled { get; set; } = false;

    public string Frequency { get; set; } = "weekly";

    public DateTime? LastSentAt { get; set; }

    public DateTime? NextSendAt { get; set; }

    public bool IncludeDashboard { get; set; } = true;
    public bool IncludeFinancial { get; set; } = true;
    public bool IncludePerformance { get; set; } = true;
    public bool IncludeClients { get; set; } = true;
    public bool IncludeProjects { get; set; } = true;
    public bool IncludeEmployees { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
