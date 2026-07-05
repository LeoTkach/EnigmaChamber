using EnigmaChamber.Web.Models;
using EnigmaChamber.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnigmaChamber.Web.Pages;

public class IndexModel(IBookingService bookings) : PageModel
{
    public IReadOnlyList<Models.Booking> TodayBookings { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["ActiveNav"] = "home";
        TodayBookings = await bookings.GetTodayAsync(ct);
    }

    public async Task<IActionResult> OnPostStartAsync(int id, CancellationToken ct)
    {
        var error = await bookings.StartAsync(id, ct);
        if (error is not null) TempData["Error"] = error;
        return RedirectToPage();
    }

    public IActionResult OnPostComplete(int id)
    {
        // status will become Completed after saving game result
        return RedirectToPage("/Results/Entry", new { bookingId = id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id, CancellationToken ct)
    {
        await bookings.CancelAsync(id, ct);
        return RedirectToPage();
    }
}
