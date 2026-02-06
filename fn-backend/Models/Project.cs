namespace fn_backend.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public bool IsActive { get; set; } = true;

    public Client? Client { get; set; }

    // CÓDIGO FUTURO - Relación con servicios deshabilitada
    // Descomentar cuando se requiera reactivar la funcionalidad de servicios
    // public ICollection<Service> Services { get; set; } = new List<Service>();
}