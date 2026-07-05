using System.ComponentModel.DataAnnotations;

namespace EnigmaChamber.Web.Models;

public class Booking
{
    public int Id { get; set; }

    [Display(Name = "Room")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Please specify customer name")]
    [StringLength(100)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please specify phone number")]
    [Phone(ErrorMessage = "Invalid phone format")]
    [Display(Name = "Phone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Range(1, 12, ErrorMessage = "Players count must be between 1 and 12")]
    [Display(Name = "Players")]
    public int PlayerCount { get; set; }

    [Required]
    [Display(Name = "Start Time")]
    public DateTime ScheduledAt { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = BookingStatuses.Pending;

    [Display(Name = "Game Master")]
    public int? GameMasterId { get; set; }

    [Display(Name = "Actor")]
    public int? ActorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Room Room { get; set; } = null!;
    public Staff? GameMaster { get; set; }
    public Staff? Actor { get; set; }
    public RunResult? RunResult { get; set; }
}
