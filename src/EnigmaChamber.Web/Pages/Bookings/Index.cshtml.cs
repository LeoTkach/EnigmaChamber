using System.ComponentModel.DataAnnotations;
using EnigmaChamber.Web.Data;
using EnigmaChamber.Web.Models;
using EnigmaChamber.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EnigmaChamber.Web.Pages.Bookings;

public class IndexModel(IBookingService bookings, IRoomService rooms, AppDbContext db) : PageModel
{
    public IReadOnlyList<Models.Booking> Items { get; private set; } = [];
    public SelectList RoomOptions { get; private set; } = null!;
    public SelectList GameMasterOptions { get; private set; } = null!;
    public SelectList ActorOptions { get; private set; } = null!;

    public string ViewMode { get; set; } = "Schedule";
    public int? SelectedRoomId { get; set; }
    public IReadOnlyList<Models.Room> Rooms { get; private set; } = [];

    public Dictionary<DateTime, List<FreeSlot>> ScheduleSlots { get; private set; } = [];
    public DateTime ScheduleStartDate { get; private set; }

    [BindProperty]
    public BookingInput Input { get; set; } = new();

    public bool ShowModal { get; private set; }

    public async Task OnGetAsync(string mode = "Schedule", int? roomId = null, bool openCreate = false, CancellationToken ct = default)
    {
        ViewMode = mode == "Journal" ? "Journal" : "Schedule";
        SelectedRoomId = roomId;
        await LoadAsync(ct);
        Input.ScheduledAt = DateTime.Today.AddDays(1).AddHours(15);
        if (SelectedRoomId.HasValue) Input.RoomId = SelectedRoomId.Value;
        ShowModal = openCreate;
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken ct)
    {
        ViewMode = "Schedule";
        SelectedRoomId = Input.RoomId;
        await LoadAsync(ct);

        if (!ModelState.IsValid)
        {
            ShowModal = true;
            return Page();
        }

        var room = Rooms.FirstOrDefault(r => r.Id == Input.RoomId);
        if (room is not null && (Input.PlayerCount < room.MinPlayers || Input.PlayerCount > room.MaxPlayers))
        {
            ModelState.AddModelError(string.Empty,
                $"Room '{room.Name}' allows between {room.MinPlayers} and {room.MaxPlayers} players.");
            ShowModal = true;
            return Page();
        }

        try
        {
            await bookings.CreateAsync(new Models.Booking
            {
                RoomId = Input.RoomId,
                CustomerName = Input.CustomerName,
                CustomerPhone = Input.CustomerPhone,
                PlayerCount = Input.PlayerCount,
                ScheduledAt = Input.ScheduledAt,
                GameMasterId = Input.GameMasterId,
                ActorId = Input.ActorId
            }, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ShowModal = true;
            return Page();
        }

        return RedirectToPage(new { mode = ViewMode, roomId = SelectedRoomId });
    }

    public async Task<IActionResult> OnPostAssignAsync(int id, int? gameMasterId, int? actorId, CancellationToken ct)
    {
        try
        {
            await bookings.AssignStaffAsync(id, gameMasterId, actorId, ct);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage(new { mode = "Journal" });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, CancellationToken ct)
    {
        await bookings.CancelAsync(id, ct);
        return RedirectToPage(new { mode = "Journal" });
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        ViewData["ActiveNav"] = "bookings";
        Rooms = await rooms.GetAllAsync(ct);
        RoomOptions = new SelectList(Rooms, nameof(Models.Room.Id), nameof(Models.Room.Name));

        var staff = await db.Staff.Where(s => s.IsActive).OrderBy(s => s.Name).AsNoTracking().ToListAsync(ct);
        GameMasterOptions = new SelectList(staff.Where(s => s.Role == StaffRoles.GameMaster), "Id", "Name");
        ActorOptions = new SelectList(staff.Where(s => s.Role == StaffRoles.Actor), "Id", "Name");

        if (ViewMode == "Journal")
        {
            Items = await bookings.GetAllAsync(ct);
        }
        else
        {
            ScheduleStartDate = DateTime.Today;
            if (Rooms.Count > 0)
            {
                SelectedRoomId ??= Rooms[0].Id;

                var slots = await bookings.GetFreeSlotsAsync(SelectedRoomId.Value, ScheduleStartDate, ScheduleStartDate.AddDays(7), ct);

                ScheduleSlots = slots
                    .GroupBy(s => s.StartTime.Date)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
        }
    }

    public class BookingInput
    {
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        [Display(Name = "Phone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Range(1, 12, ErrorMessage = "From 1 to 12 players")]
        [Display(Name = "Players")]
        public int PlayerCount { get; set; } = 4;

        [Required]
        [Display(Name = "Start Time")]
        public DateTime ScheduledAt { get; set; }

        [Display(Name = "Game Master")]
        public int? GameMasterId { get; set; }

        [Display(Name = "Actor")]
        public int? ActorId { get; set; }
    }
}
