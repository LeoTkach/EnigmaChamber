using System;
using EnigmaChamber.Web.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnigmaChamber.Web.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260704000000_InitialCreate")]
/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditLog",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TableName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                RecordId = table.Column<int>(type: "int", nullable: false),
                Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table => table.PrimaryKey("PK_AuditLog", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Rooms",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                MaxPlayers = table.Column<int>(type: "int", nullable: false),
                DurationMinutes = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table => table.PrimaryKey("PK_Rooms", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Bookings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RoomId = table.Column<int>(type: "int", nullable: false),
                CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                CustomerPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                PlayerCount = table.Column<int>(type: "int", nullable: false),
                ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Bookings", x => x.Id);
                table.ForeignKey(name: "FK_Bookings_Rooms_RoomId", column: x => x.RoomId, principalTable: "Rooms", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Puzzles",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                RoomId = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                HintText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                OrderIndex = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Puzzles", x => x.Id);
                table.ForeignKey(name: "FK_Puzzles_Rooms_RoomId", column: x => x.RoomId, principalTable: "Rooms", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RunResults",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                BookingId = table.Column<int>(type: "int", nullable: false),
                Success = table.Column<bool>(type: "bit", nullable: false),
                ElapsedMinutes = table.Column<int>(type: "int", nullable: false),
                HintsUsed = table.Column<int>(type: "int", nullable: false),
                FinalMinutes = table.Column<int>(type: "int", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RunResults", x => x.Id);
                table.ForeignKey(name: "FK_RunResults_Bookings_BookingId", column: x => x.BookingId, principalTable: "Bookings", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Bookings_RoomId_ScheduledAt", table: "Bookings", columns: new[] { "RoomId", "ScheduledAt" });
        migrationBuilder.CreateIndex(name: "IX_RunResults_BookingId", table: "RunResults", column: "BookingId", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditLog");
        migrationBuilder.DropTable(name: "Puzzles");
        migrationBuilder.DropTable(name: "RunResults");
        migrationBuilder.DropTable(name: "Bookings");
        migrationBuilder.DropTable(name: "Rooms");
    }
}
