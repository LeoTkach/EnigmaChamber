using System.Globalization;
using EnigmaChamber.Web.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EnigmaChamber.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var english = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = english;
        CultureInfo.DefaultThreadCurrentUICulture = english;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/enigmachamber-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            builder.Services.AddRazorPages();
            builder.Services.AddDbContext<Data.AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<Services.IRoomService, Services.RoomService>();
            builder.Services.AddScoped<Services.IBookingService, Services.BookingService>();
            builder.Services.AddScoped<Services.IResultsService, Services.ResultsService>();
            builder.Services.AddMemoryCache();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
                db.Database.Migrate();
                Data.DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.MapRazorPages();
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
