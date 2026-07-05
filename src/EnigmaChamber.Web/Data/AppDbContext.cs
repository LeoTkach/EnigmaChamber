using EnigmaChamber.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EnigmaChamber.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<RunResult> RunResults => Set<RunResult>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<Staff> Staff => Set<Staff>();

    public IQueryable<FreeSlot> GetFreeSlots(int roomId, DateTime dateFrom, DateTime dateTo) =>
        FromExpression(() => GetFreeSlots(roomId, dateFrom, dateTo));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FreeSlot>().HasNoKey().ToView(null);
        modelBuilder.HasDbFunction(typeof(AppDbContext).GetMethod(nameof(GetFreeSlots))!)
            .HasName("fn_FreeSlots");


        modelBuilder.Entity<Room>(e =>
        {
            e.ToTable("Rooms");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Booking>(e =>
        {
            // audit trigger requires disabling OUTPUT clause in EF
            e.ToTable("Bookings", t => t.HasTrigger("trg_Bookings_Audit"));
            e.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
            e.Property(x => x.CustomerPhone).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue(BookingStatuses.Pending);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => new { x.RoomId, x.ScheduledAt });
            e.HasOne(x => x.Room).WithMany(x => x.Bookings).HasForeignKey(x => x.RoomId);
            e.HasOne(x => x.GameMaster).WithMany(x => x.GameMasterBookings).HasForeignKey(x => x.GameMasterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Actor).WithMany(x => x.ActorBookings).HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RunResult>(e =>
        {
            e.ToTable("RunResults", t => t.HasTrigger("trg_RunResults_SetFinalTime"));
            e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.CompletedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.BookingId).IsUnique();
            e.HasOne(x => x.Booking).WithOne(x => x.RunResult).HasForeignKey<RunResult>(x => x.BookingId);
        });

        modelBuilder.Entity<AuditLogEntry>(e =>
        {
            e.ToTable("AuditLog");
            e.Property(x => x.TableName).HasMaxLength(64).IsRequired();
            e.Property(x => x.Action).HasMaxLength(32).IsRequired();
            e.Property(x => x.ChangedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Staff>(e =>
        {
            e.ToTable("Staff", t => t.HasCheckConstraint("CK_Staff_Role", $"Role IN ('{StaffRoles.GameMaster}', '{StaffRoles.Actor}')"));
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });
        // result of cursor procedure sp_MonthlyStats, not mapped to table
        modelBuilder.Entity<MonthlyStatRow>().HasNoKey().ToView(null);
    }
}
