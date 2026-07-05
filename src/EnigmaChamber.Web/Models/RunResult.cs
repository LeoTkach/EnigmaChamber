using System.ComponentModel.DataAnnotations;

namespace EnigmaChamber.Web.Models;

public class RunResult
{
    public int Id { get; set; }
    public int BookingId { get; set; }

    [Display(Name = "Success")]
    public bool Success { get; set; }

    [Range(0, 300)]
    [Display(Name = "Elapsed (min)")]
    public int ElapsedMinutes { get; set; }

    [Range(0, 20)]
    [Display(Name = "Hints Used")]
    public int HintsUsed { get; set; }

    public int? FinalMinutes { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public Booking Booking { get; set; } = null!;
}
