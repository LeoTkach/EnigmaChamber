using System.ComponentModel.DataAnnotations;

namespace EnigmaChamber.Web.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxPlayers { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Range(1, 10, ErrorMessage = "Min players must be greater than 0")]
    public int MinPlayers { get; set; } = 2;

    [Range(1, 10, ErrorMessage = "Difficulty must be from 1 to 5")]
    public int Difficulty { get; set; } = 3;

    [Range(0, 100000, ErrorMessage = "Price cannot be negative")]
    public decimal Price { get; set; }

    [Range(0, 100, ErrorMessage = "Min age from 0")]
    public int MinAge { get; set; }

    public bool HasActor { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
}
