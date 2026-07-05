using EnigmaChamber.Web.Data;
using EnigmaChamber.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EnigmaChamber.Web.Pages.Audit;

public class IndexModel(AppDbContext db) : PageModel
{
    public IReadOnlyList<AuditLogEntry> Items { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        ViewData["ActiveNav"] = "audit";
        Items = await db.AuditLog
            .OrderByDescending(a => a.ChangedAt)
            .Take(50)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
