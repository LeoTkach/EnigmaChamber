using EnigmaChamber.Web.Data;
using EnigmaChamber.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EnigmaChamber.Web.Pages.Staff;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Models.Staff> StaffList { get; set; } = [];
    
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public async Task OnGetAsync()
    {
        StaffList = await db.Staff.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (Input.Id == 0)
        {
            db.Staff.Add(new Models.Staff
            {
                Name = Input.Name,
                Role = Input.Role,
                IsActive = Input.IsActive
            });
        }
        else
        {
            var s = await db.Staff.FindAsync(Input.Id);
            if (s != null)
            {
                s.Name = Input.Name;
                s.Role = Input.Role;
                s.IsActive = Input.IsActive;
            }
        }
        
        await db.SaveChangesAsync();
        return RedirectToPage();
    }
}
