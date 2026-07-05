using System.ComponentModel.DataAnnotations;

namespace EnigmaChamber.Web.Models;

public class Staff
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please specify staff name")]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(50)]
    [Display(Name = "Role")]
    public string Role { get; set; } = StaffRoles.GameMaster;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for Bookings
    public ICollection<Booking> GameMasterBookings { get; set; } = [];
    public ICollection<Booking> ActorBookings { get; set; } = [];
}
