using System.ComponentModel.DataAnnotations;
using EnigmaChamber.Web.Models;
using EnigmaChamber.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnigmaChamber.Web.Pages.Results;

public class EntryModel(IBookingService bookings, IResultsService results) : PageModel
{
    public Models.Booking? Booking { get; private set; }

    [BindProperty]
    public ResultInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int bookingId, CancellationToken ct)
    {
        ViewData["ActiveNav"] = "results";
        Booking = await bookings.GetByIdAsync(bookingId, ct);
        if (Booking is null) return RedirectToPage("/Index");

        Input.BookingId = bookingId;
        Input.ElapsedMinutes = Booking.Room?.DurationMinutes ?? 60;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["ActiveNav"] = "results";
        Booking = await bookings.GetByIdAsync(Input.BookingId, ct);
        if (Booking is null) return RedirectToPage("/Index");

        if (!ModelState.IsValid) return Page();

        try
        {
            await results.SubmitAsync(Input.BookingId, Input.Success, Input.ElapsedMinutes, Input.HintsUsed, Input.Notes, ct);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }

        return RedirectToPage("/Results/Index");
    }

    public class ResultInput
    {
        public int BookingId { get; set; }

        [Display(Name = "Team escaped successfully")]
        public bool Success { get; set; } = true;

        [Range(1, 300, ErrorMessage = "Time between 1 and 300 minutes")]
        [Display(Name = "Elapsed Time (min)")]
        public int ElapsedMinutes { get; set; }

        [Range(0, 20, ErrorMessage = "Hints between 0 and 20")]
        [Display(Name = "Hints used")]
        public int HintsUsed { get; set; }

        [StringLength(500)]
        [Display(Name = "Game Master notes")]
        public string? Notes { get; set; }
    }
}
