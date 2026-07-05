using EnigmaChamber.Web.Data;
using EnigmaChamber.Web.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EnigmaChamber.Web.Services;

public interface IBookingService
{
    Task<IReadOnlyList<Booking>> GetTodayAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default);
    Task<Booking?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default);
    Task AssignStaffAsync(int id, int? gameMasterId, int? actorId, CancellationToken ct = default);
    Task<string?> StartAsync(int id, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<FreeSlot>> GetFreeSlotsAsync(int roomId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default);
}

public class BookingService(AppDbContext db, ILogger<BookingService> logger) : IBookingService
{
    public async Task<IReadOnlyList<Booking>> GetTodayAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return await db.Bookings
            .Include(b => b.Room)
            .Include(b => b.GameMaster)
            .Include(b => b.Actor)
            .Where(b => b.ScheduledAt >= today && b.ScheduledAt < tomorrow)
            .OrderBy(b => b.ScheduledAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken ct = default) =>
        await db.Bookings
            .Include(b => b.Room)
            .Include(b => b.GameMaster)
            .Include(b => b.Actor)
            .OrderByDescending(b => b.ScheduledAt)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<Booking?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default)
    {
        var bookingIdParam = new SqlParameter
        {
            ParameterName = "@BookingId",
            SqlDbType = System.Data.SqlDbType.Int,
            Direction = System.Data.ParameterDirection.Output
        };

        try
        {
            // creation goes through stored procedure: slot validation on SQL Server side
            await db.Database.ExecuteSqlRawAsync(
                "EXEC sp_CreateBooking @RoomId, @CustomerName, @CustomerPhone, @PlayerCount, @ScheduledAt, @BookingId OUTPUT",
                [
                    new SqlParameter("@RoomId", booking.RoomId),
                    new SqlParameter("@CustomerName", booking.CustomerName),
                    new SqlParameter("@CustomerPhone", booking.CustomerPhone),
                    new SqlParameter("@PlayerCount", booking.PlayerCount),
                    new SqlParameter("@ScheduledAt", booking.ScheduledAt),
                    bookingIdParam
                ], ct);

            booking.Id = (int)bookingIdParam.Value;

            if (booking.GameMasterId is not null || booking.ActorId is not null)
                await AssignStaffAsync(booking.Id, booking.GameMasterId, booking.ActorId, ct);

            logger.LogInformation("Booking created: {BookingId} room {RoomId} via SP", booking.Id, booking.RoomId);
            return booking;
        }
        catch (SqlException ex) when (ex.Number is 50001 or 50002)
        {
            logger.LogWarning("Booking rejected by SQL: {Message}", ex.Message);
            throw new InvalidOperationException(ex.Message);
        }
    }

    public async Task AssignStaffAsync(int id, int? gameMasterId, int? actorId, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Booking #{id} not found.");
        booking.GameMasterId = gameMasterId;
        booking.ActorId = actorId;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Booking {BookingId}: staff assigned GM={GmId} Actor={ActorId}", id, gameMasterId, actorId);
    }

    /// <summary>Returns null on success or error text if game cannot be started.</summary>
    public async Task<string?> StartAsync(int id, CancellationToken ct = default)
    {
        var booking = await db.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (booking is null || booking.Status != BookingStatuses.Pending)
            return "Game can only be started for a pending booking.";

        if (booking.GameMasterId is null)
            return "Assign a game master first.";

        if (booking.Room.HasActor && booking.ActorId is null)
            return "This room requires an actor - assign one before starting.";

        booking.Status = BookingStatuses.InProgress;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Booking {BookingId} started", id);
        return null;
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        var booking = await db.Bookings.FindAsync([id], ct);
        if (booking is null || booking.Status is not (BookingStatuses.Pending or BookingStatuses.InProgress)) return;
        booking.Status = BookingStatuses.Cancelled;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Booking {BookingId} cancelled", id);
    }

    public async Task<IReadOnlyList<FreeSlot>> GetFreeSlotsAsync(int roomId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default)
    {
        return await db.GetFreeSlots(roomId, dateFrom, dateTo)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }
}
