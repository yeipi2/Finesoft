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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
