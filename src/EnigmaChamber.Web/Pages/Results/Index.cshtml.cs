using EnigmaChamber.Web.Models;
using EnigmaChamber.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EnigmaChamber.Web.Pages.Results;

public class IndexModel(IResultsService resultService, IBookingService bookingService, IRoomService roomService) : PageModel
{
    [BindProperty]
    public ResultInputModel Input { get; set; } = new();

    public SelectList BookingOptions { get; set; } = null!;
    public List<RunResult> HallOfFame { get; set; } = new();
    public IReadOnlyList<Room> Rooms { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? SelectedRoomId { get; set; }

    public class ResultInputModel
    {
        public int BookingId { get; set; }
        public bool Success { get; set; }
        public int ElapsedMinutes { get; set; }
        public int HintsUsed { get; set; }
        public string? Notes { get; set; }
    }

    public async Task OnGetAsync()
    {
        ViewData["ActiveNav"] = "results";
        await LoadDataAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDataAsync();
            return Page();
        }

        try
        {
            await resultService.SubmitAsync(Input.BookingId, Input.Success, Input.ElapsedMinutes, Input.HintsUsed, Input.Notes);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadDataAsync();
            return Page();
        }

        return RedirectToPage("./Index", new { SelectedRoomId });
    }

    private async Task LoadDataAsync()
    {
        var inProgress = (await bookingService.GetAllAsync())
            .Where(b => b.Status == BookingStatuses.InProgress);
        BookingOptions = new SelectList(inProgress.Select(b => new {
            b.Id,
            Display = $"{b.ScheduledAt:HH:mm} | {b.Room?.Name} | {b.CustomerName}"
        }), "Id", "Display");

        Rooms = await roomService.GetAllAsync();
        if (Rooms.Any() && !SelectedRoomId.HasValue)
        {
            SelectedRoomId = Rooms.First().Id;
        }

        if (SelectedRoomId.HasValue)
        {
            HallOfFame = (await resultService.GetHallOfFameAsync(10))
                .Where(r => r.Booking.RoomId == SelectedRoomId.Value)
                .ToList();
        }
    }
}
