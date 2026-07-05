using System.ComponentModel.DataAnnotations;
using EnigmaChamber.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnigmaChamber.Web.Pages.Rooms;

public class IndexModel(IRoomService rooms) : PageModel
{
    public IReadOnlyList<Models.Room> Items { get; private set; } = [];

    [BindProperty]
    public RoomInput Input { get; set; } = new();

    public bool ShowModal { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ShowModal = true;
            await LoadAsync(ct);
            return Page();
        }

        if (Input.MinPlayers > Input.MaxPlayers)
        {
            ModelState.AddModelError(string.Empty, "Min players cannot exceed max players.");
            ShowModal = true;
            await LoadAsync(ct);
            return Page();
        }

        if (Input.Id == 0)
        {
            await rooms.CreateAsync(new Models.Room
            {
                Name = Input.Name,
                Description = Input.Description,
                MinPlayers = Input.MinPlayers,
                MaxPlayers = Input.MaxPlayers,
                DurationMinutes = Input.DurationMinutes,
                Difficulty = Input.Difficulty,
                Price = Input.Price,
                MinAge = Input.MinAge,
                HasActor = Input.HasActor,
                IsActive = Input.IsActive
            }, ct);
        }
        else
        {
            var room = await rooms.GetByIdAsync(Input.Id, ct);
            if (room is null) return NotFound();

            room.Name = Input.Name;
            room.Description = Input.Description;
            room.MinPlayers = Input.MinPlayers;
            room.MaxPlayers = Input.MaxPlayers;
            room.DurationMinutes = Input.DurationMinutes;
            room.Difficulty = Input.Difficulty;
            room.Price = Input.Price;
            room.MinAge = Input.MinAge;
            room.HasActor = Input.HasActor;
            room.IsActive = Input.IsActive;
            await rooms.UpdateAsync(room, ct);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        await rooms.DeleteAsync(id, ct);
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        ViewData["ActiveNav"] = "rooms";
        Items = await rooms.GetAllAsync(ct);
    }

    public class RoomInput
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Range(1, 12, ErrorMessage = "From 1 to 12 players")]
        [Display(Name = "Min Players")]
        public int MinPlayers { get; set; } = 2;

        [Range(2, 12, ErrorMessage = "From 2 to 12 players")]
        [Display(Name = "Max Players")]
        public int MaxPlayers { get; set; } = 6;

        [Range(30, 120, ErrorMessage = "Duration must be 30-120 min")]
        [Display(Name = "Duration (min)")]
        public int DurationMinutes { get; set; } = 60;

        [Range(1, 5, ErrorMessage = "Difficulty is 1-5")]
        [Display(Name = "Difficulty")]
        public int Difficulty { get; set; } = 3;

        [Range(0, 100000, ErrorMessage = "Price cannot be negative")]
        [Display(Name = "Price per game")]
        public decimal Price { get; set; } = 1000;

        [Range(0, 21, ErrorMessage = "Age 0-21")]
        [Display(Name = "Min Age")]
        public int MinAge { get; set; } = 12;

        [Display(Name = "Requires Actor")]
        public bool HasActor { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
