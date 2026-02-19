using System.ComponentModel.DataAnnotations.Schema;

namespace fn_backend.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // NUEVA PROPIEDAD: Tarifa por hora para el proyecto.
    // Se utiliza en QuoteService para calcular el costo de un ticket:
    //    var ticketCost = ticket.ActualHours * ticket.Project.HourlyRate;
    public decimal HourlyRate { get; set; } = 0.0m;

    public int ClientId { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey("ClientId")]  // ⭐ AGREGAR ESTO
    public Client? Client { get; set; }

    // CÓDIGO FUTURO - Relación con servicios deshabilitada
    // Descomentar cuando se requiera reactivar la funcionalidad de servicios
    // public ICollection<Service> Services { get; set; } = new List<Service>();
}