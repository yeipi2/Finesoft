using System.ComponentModel.DataAnnotations;

namespace fn_backend.DTO;

public class ServiceDto
{
    public int? Id { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Required] public decimal HourlyRate { get; set; }
    [Required] public int ProjectId { get; set; }
    [Required] public int TypeServiceId { get; set; }
    [Required] public int TypeActivityId { get; set; }
    public bool IsActive { get; set; } = true;
}