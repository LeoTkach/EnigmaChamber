using EnigmaChamber.Web.Data;
using EnigmaChamber.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EnigmaChamber.Web.Services;

public interface IResultsService
{
    /// <summary>Records game result and closes booking.</summary>
    Task<RunResult> SubmitAsync(int bookingId, bool success, int elapsedMinutes, int hintsUsed, string? notes, CancellationToken ct = default);

    /// <summary>Top successful runs for each room (hall of fame).</summary>
    Task<IReadOnlyList<RunResult>> GetHallOfFameAsync(int topPerRoom = 5, CancellationToken ct = default);

    Task<IReadOnlyList<RunResult>> GetRecentAsync(int count = 20, CancellationToken ct = default);

    Task<List<MonthlyStatRow>> GetMonthlyStatsAsync(int year, int month);
}

public class ResultsService(AppDbContext db, ILogger<ResultsService> logger) : IResultsService
{
    public async Task<RunResult> SubmitAsync(int bookingId, bool success, int elapsedMinutes, int hintsUsed, string? notes, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(b => b.RunResult)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new InvalidOperationException($"Booking #{bookingId} not found.");

        if (booking.RunResult is not null)
            throw new InvalidOperationException("Result for this game is already recorded.");

        var result = new RunResult
        {
            BookingId = bookingId,
            Success = success,
            ElapsedMinutes = elapsedMinutes,
            HintsUsed = hintsUsed,
            Notes = notes
        };

        db.RunResults.Add(result);
        booking.Status = BookingStatuses.Completed;
        await db.SaveChangesAsync(ct);

        // FinalMinutes is calculated by trigger trg_RunResults_SetFinalTime via fn_FinalTime
        await db.Entry(result).ReloadAsync(ct);

        logger.LogInformation("Run result saved: booking {BookingId}, success {Success}, final {FinalMinutes} min",
            bookingId, success, result.FinalMinutes);
        return result;
    }

    public async Task<IReadOnlyList<RunResult>> GetHallOfFameAsync(int topPerRoom = 5, CancellationToken ct = default)
    {
        var all = await db.RunResults
            .Include(r => r.Booking).ThenInclude(b => b.Room)
            .Where(r => r.Success)
            .AsNoTracking()
            .ToListAsync(ct);

        return all
            .GroupBy(r => r.Booking.RoomId)
            .SelectMany(g => g.OrderBy(r => r.FinalMinutes ?? int.MaxValue).Take(topPerRoom))
            .ToList();
    }

    public async Task<IReadOnlyList<RunResult>> GetRecentAsync(int count = 20, CancellationToken ct = default) =>
        await db.RunResults
            .Include(r => r.Booking).ThenInclude(b => b.Room)
            .OrderByDescending(r => r.CompletedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<List<MonthlyStatRow>> GetMonthlyStatsAsync(int year, int month)
    {
        return await db.Set<MonthlyStatRow>()
            .FromSqlInterpolated($"EXEC sp_MonthlyStats @Year = {year}, @Month = {month}")
            .ToListAsync();
    }
}
