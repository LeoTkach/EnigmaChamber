using EnigmaChamber.Web.Data;
using EnigmaChamber.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EnigmaChamber.Web.Services;

public interface IRoomService
{
    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default);
    Task<Room?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Room> CreateAsync(Room room, CancellationToken ct = default);
    Task UpdateAsync(Room room, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class RoomService(AppDbContext db, IMemoryCache cache, ILogger<RoomService> logger) : IRoomService
{
    private const string CacheKey = "rooms:all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<Room>? cached) && cached is not null)
            return cached;

        var rooms = await db.Rooms.OrderBy(r => r.Name).AsNoTracking().ToListAsync(ct);
        cache.Set(CacheKey, (IReadOnlyList<Room>)rooms, CacheTtl);
        logger.LogInformation("Rooms cache refreshed: {Count} rooms", rooms.Count);
        return rooms;
    }

    public async Task<Room?> GetByIdAsync(int id, CancellationToken ct = default) =>
        await db.Rooms.FindAsync([id], ct);

    public async Task<Room> CreateAsync(Room room, CancellationToken ct = default)
    {
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
        logger.LogInformation("Room created: {RoomId} {RoomName}", room.Id, room.Name);
        return room;
    }

    public async Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        db.Rooms.Update(room);
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
        logger.LogInformation("Room updated: {RoomId}", room.Id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var room = await db.Rooms.FindAsync([id], ct);
        if (room is null) return;
        db.Rooms.Remove(room);
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
        logger.LogInformation("Room deleted: {RoomId}", id);
    }
}
