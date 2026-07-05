using EnigmaChamber.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EnigmaChamber.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Staff.AnyAsync())
        {
            db.Staff.AddRange(
                new Staff { Name = "Alexander Smith", Role = StaffRoles.GameMaster },
                new Staff { Name = "Maria Johnson", Role = StaffRoles.GameMaster },
                new Staff { Name = "David Bond", Role = StaffRoles.GameMaster },
                new Staff { Name = "Igor Fox", Role = StaffRoles.Actor },
                new Staff { Name = "Elena Miller", Role = StaffRoles.Actor }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Rooms.AnyAsync())
        {
            db.Rooms.AddRange(
                new Room { Name = "Laboratory #7", Description = "The missing professor left behind encrypted clues.", MinPlayers = 2, MaxPlayers = 6, DurationMinutes = 60, Difficulty = 3, Price = 120, MinAge = 12, HasActor = false },
                new Room { Name = "Pirate's Cabin", Description = "Find the chest key before the storm ends.", MinPlayers = 2, MaxPlayers = 5, DurationMinutes = 75, Difficulty = 2, Price = 100, MinAge = 8, HasActor = false },
                new Room { Name = "Lost Archive", Description = "Uncover the mystery of the ancient vault.", MinPlayers = 3, MaxPlayers = 4, DurationMinutes = 90, Difficulty = 5, Price = 180, MinAge = 16, HasActor = true });
            await db.SaveChangesAsync();
        }

        if (!await db.Bookings.AnyAsync())
        {
            var rooms = await db.Rooms.OrderBy(r => r.Id).ToListAsync();
            if (rooms.Count == 0) return;

            int RoomAt(int i) => rooms[i % rooms.Count].Id;

            var gms = await db.Staff.Where(s => s.Role == StaffRoles.GameMaster).Select(s => s.Id).ToListAsync();
            var actors = await db.Staff.Where(s => s.Role == StaffRoles.Actor).Select(s => s.Id).ToListAsync();

            int? GmAt(int i) => gms.Count > 0 ? gms[i % gms.Count] : null;
            int? ActorAt(int i) => actors.Count > 0 ? actors[i % actors.Count] : null;

            var today = DateTime.Today;

            // сьогоднішні активні бронювання
            db.Bookings.AddRange(
                new Booking
                {
                    RoomId = RoomAt(0),
                    GameMasterId = GmAt(0),
                    CustomerName = "Orion Team",
                    CustomerPhone = "+380501112233",
                    PlayerCount = 4,
                    ScheduledAt = today.AddHours(15),
                    Status = BookingStatuses.Pending
                },
                new Booking
                {
                    RoomId = RoomAt(1),
                    CustomerName = "Kovalenko Family",
                    CustomerPhone = "+380671234567",
                    PlayerCount = 5,
                    ScheduledAt = today.AddHours(18),
                    Status = BookingStatuses.Pending
                },
                new Booking
                {
                    RoomId = RoomAt(2),
                    GameMasterId = GmAt(1),
                    ActorId = ActorAt(0),
                    CustomerName = "Maria's Birthday",
                    CustomerPhone = "+380637654321",
                    PlayerCount = 4,
                    ScheduledAt = today.AddHours(20),
                    Status = BookingStatuses.InProgress
                });

            // історія завершених ігор: наповнює зал слави, статистику та аудит
            (int daysAgo, int hour, int roomIdx, string team, string phone, int players, bool success, int minutes, int hints, string? notes)[] history =
            [
                (1,  15, 0, "Students Group",   "+380931112233", 3, true,  58, 2, "Escaped at the last second"),
                (2,  17, 1, "Night Owls",       "+380671000001", 4, true,  61, 0, "Clean game without hints"),
                (3,  19, 2, "Dream Team",       "+380671000002", 4, false, 90, 5, "Failed to open the archive"),
                (5,  12, 0, "Rocket Crew",      "+380671000003", 5, true,  47, 1, null),
                (7,  16, 1, "Kyiv Wanderers",   "+380671000004", 3, true,  70, 3, null),
                (9,  20, 2, "The Detectives",   "+380671000005", 4, true,  82, 2, "Strong team"),
                (12, 14, 0, "Alpha Squad",      "+380671000006", 6, false, 60, 4, "Gave up on the last puzzle"),
                (16, 18, 1, "Puzzle Hunters",   "+380671000007", 2, true,  66, 1, null),
                (21, 15, 2, "Escape Legends",   "+380671000008", 4, true,  75, 0, "Record pace"),
                (26, 17, 0, "Brainstormers",    "+380671000009", 4, true,  52, 2, null),
                (33, 16, 1, "The Curious",      "+380671000010", 5, false, 75, 6, "Too many hints requested"),
                (38, 19, 2, "Vault Breakers",   "+380671000011", 3, true,  88, 3, null)
            ];

            var idx = 0;
            foreach (var h in history)
            {
                var roomId = RoomAt(h.roomIdx);
                var needsActor = rooms.First(r => r.Id == roomId).HasActor;
                db.Bookings.Add(new Booking
                {
                    RoomId = roomId,
                    GameMasterId = GmAt(idx),
                    ActorId = needsActor ? ActorAt(idx) : null,
                    CustomerName = h.team,
                    CustomerPhone = h.phone,
                    PlayerCount = h.players,
                    ScheduledAt = today.AddDays(-h.daysAgo).AddHours(h.hour),
                    Status = BookingStatuses.Completed,
                    RunResult = new RunResult
                    {
                        Success = h.success,
                        ElapsedMinutes = h.minutes,
                        HintsUsed = h.hints,
                        Notes = h.notes,
                        CompletedAt = today.AddDays(-h.daysAgo).AddHours(h.hour + 2)
                    }
                });
                idx++;
            }

            await db.SaveChangesAsync();
        }
    }
}
