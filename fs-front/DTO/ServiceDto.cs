using System.ComponentModel.DataAnnotations;

namespace fs_front.DTO;

public class ServiceDto
{
    public int? Id { get; set; }
    
    [Required(ErrorMessage = "El nombre del servicio es obligatorio")]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La tarifa por hora es obligatoria")]
    [Range(0.01, double.MaxValue, ErrorMessage = "La tarifa debe ser mayor a 0")]
    public decimal HourlyRate { get; set; }
    
    [Required(ErrorMessage = "El proyecto es obligatorio")]
    public int ProjectId { get; set; }
    
    [Required(ErrorMessage = "El tipo de servicio es obligatorio")]
    public int TypeServiceId { get; set; }
    
    [Required(ErrorMessage = "El tipo de actividad es obligatorio")]
    public int TypeActivityId { get; set; }
    
    public bool IsActive { get; set; } = true;
}