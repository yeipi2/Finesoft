namespace fs_front.DTO;

public class ServiceDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }
    
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    
    public int TypeServiceId { get; set; }
    public string TypeServiceName { get; set; } = string.Empty;
    
    public int TypeActivityId { get; set; }
    public string TypeActivityName { get; set; } = string.Empty;
}