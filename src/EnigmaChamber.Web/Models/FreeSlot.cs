namespace EnigmaChamber.Web.Models;

public class FreeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public int? BookingId { get; set; }
    public string? CustomerName { get; set; }
    public string? Status { get; set; }
}
