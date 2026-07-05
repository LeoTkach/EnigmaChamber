using EnigmaChamber.Web.Models;
using EnigmaChamber.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EnigmaChamber.Web.Pages.Stats;

public class IndexModel : PageModel
{
    private readonly IResultsService _resultsService;

    public IndexModel(IResultsService resultsService)
    {
        _resultsService = resultsService;
    }

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.Now.Year;

    [BindProperty(SupportsGet = true)]
    public int Month { get; set; } = DateTime.Now.Month;

    public List<MonthlyStatRow> Stats { get; set; } = new();

    public async Task OnGetAsync()
    {
        ViewData["ActiveNav"] = "stats";
        Stats = await _resultsService.GetMonthlyStatsAsync(Year, Month);
    }
}
