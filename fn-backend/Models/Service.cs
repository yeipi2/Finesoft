namespace fn_backend.Models;

public class Service
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; }

    // Relationships
    public int ProjectId { get; set; }
    public Project Project { get; set; }

    public int TypeServiceId { get; set; }
    public TypeService TypeService { get; set; }

    public int TypeActivityId { get; set; }
    public TypeActivity TypeActivity { get; set; }
}