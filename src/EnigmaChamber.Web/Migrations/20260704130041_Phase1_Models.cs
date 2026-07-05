using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnigmaChamber.Web.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_Models : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HasActor",
                table: "Rooms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MinAge",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinPlayers",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Rooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ActorId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GameMasterId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.CheckConstraint("CK_Staff_Role", "Role IN ('GameMaster', 'Actor')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ActorId",
                table: "Bookings",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_GameMasterId",
                table: "Bookings",
                column: "GameMasterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staff_ActorId",
                table: "Bookings",
                column: "ActorId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staff_GameMasterId",
                table: "Bookings",
                column: "GameMasterId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
CREATE OR ALTER FUNCTION dbo.fn_FreeSlots
(
    @RoomId INT,
    @DateFrom DATE,
    @DateTo DATE
)
RETURNS TABLE
AS
RETURN
(
    WITH Config AS (
        SELECT 10 AS WorkStartHour, 22 AS WorkEndHour, 15 AS BufferMinutes
    ),
    Dates AS (
        SELECT CAST(@DateFrom AS DATETIME) AS CurrentDate
        UNION ALL
        SELECT DATEADD(day, 1, CurrentDate)
        FROM Dates
        WHERE CurrentDate < CAST(@DateTo AS DATETIME)
    ),
    RoomInfo AS (
        SELECT DurationMinutes FROM Rooms WHERE Id = @RoomId
    ),
    TimeSlots AS (
        SELECT 
            CAST(DATEADD(hour, (SELECT WorkStartHour FROM Config), 0) AS DATETIME) AS SlotStart,
            CAST(DATEADD(minute, (SELECT DurationMinutes FROM RoomInfo), DATEADD(hour, (SELECT WorkStartHour FROM Config), 0)) AS DATETIME) AS SlotEnd
        UNION ALL
        SELECT 
            DATEADD(minute, (SELECT DurationMinutes FROM RoomInfo) + (SELECT BufferMinutes FROM Config), SlotStart),
            DATEADD(minute, (SELECT DurationMinutes FROM RoomInfo) + (SELECT BufferMinutes FROM Config), SlotEnd)
        FROM TimeSlots
        WHERE DATEADD(minute, (SELECT DurationMinutes FROM RoomInfo) + (SELECT BufferMinutes FROM Config), SlotEnd) <= CAST(DATEADD(hour, (SELECT WorkEndHour FROM Config), 0) AS DATETIME)
    ),
    AllSlots AS (
        SELECT 
            DATEADD(day, DATEDIFF(day, 0, d.CurrentDate), t.SlotStart) AS StartTime,
            DATEADD(day, DATEDIFF(day, 0, d.CurrentDate), t.SlotEnd) AS EndTime
        FROM Dates d
        CROSS JOIN TimeSlots t
    )
    SELECT 
        s.StartTime,
        s.EndTime,
        CAST(CASE WHEN b.Id IS NULL THEN 1 ELSE 0 END AS BIT) AS IsAvailable,
        b.Id AS BookingId,
        b.CustomerName,
        b.Status
    FROM AllSlots s
    LEFT JOIN Bookings b ON b.RoomId = @RoomId 
        AND b.ScheduledAt < s.EndTime 
        AND DATEADD(minute, (SELECT DurationMinutes FROM RoomInfo) + (SELECT BufferMinutes FROM Config), b.ScheduledAt) > s.StartTime
        AND b.Status != 'Cancelled'
)
""");

            migrationBuilder.Sql("""
CREATE OR ALTER PROCEDURE dbo.sp_CreateBooking
    @RoomId INT,
    @CustomerName NVARCHAR(100),
    @CustomerPhone NVARCHAR(20),
    @PlayerCount INT,
    @ScheduledAt DATETIME,
    @BookingId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Duration INT;
        SELECT @Duration = DurationMinutes FROM Rooms WHERE Id = @RoomId;

        DECLARE @Buffer INT = 15;
        DECLARE @TotalDuration INT = @Duration + @Buffer;

        DECLARE @EndTime DATETIME = DATEADD(minute, @TotalDuration, @ScheduledAt);

        IF EXISTS (
            SELECT 1 FROM Bookings 
            WHERE RoomId = @RoomId 
            AND Status != 'Cancelled'
            AND ScheduledAt < @EndTime
            AND DATEADD(minute, @TotalDuration, ScheduledAt) > @ScheduledAt
        )
        BEGIN
            THROW 50001, 'Час вже заброньовано.', 1;
        END

        INSERT INTO Bookings (RoomId, CustomerName, CustomerPhone, PlayerCount, ScheduledAt, Status)
        VALUES (@RoomId, @CustomerName, @CustomerPhone, @PlayerCount, @ScheduledAt, 'Pending');

        SET @BookingId = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
""");

            migrationBuilder.Sql("""
CREATE OR ALTER TRIGGER trg_Bookings_Audit
ON Bookings
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Action NVARCHAR(32);
    IF EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted)
        SET @Action = 'UPDATE';
    ELSE IF EXISTS(SELECT * FROM inserted)
        SET @Action = 'INSERT';
    ELSE
        SET @Action = 'DELETE';

    DECLARE @Details NVARCHAR(MAX) = '';
    IF @Action = 'UPDATE'
    BEGIN
        SELECT TOP 1 @Details = CONCAT('Status changed to ', i.Status)
        FROM inserted i
        JOIN deleted d ON i.Id = d.Id
        WHERE i.Status != d.Status;
    END
    ELSE IF @Action = 'INSERT'
    BEGIN
        SELECT TOP 1 @Details = CONCAT('Booking created for ', i.CustomerName)
        FROM inserted i;
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        SELECT TOP 1 @Details = CONCAT('Booking deleted for ', d.CustomerName)
        FROM deleted d;
    END

    INSERT INTO AuditLog (TableName, RecordId, Action, NewValue)
    SELECT 'Bookings', COALESCE(i.Id, d.Id), @Action, @Details
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.Id = d.Id;
END
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staff_ActorId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staff_GameMasterId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ActorId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_GameMasterId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "HasActor",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MinAge",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MinPlayers",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "GameMasterId",
                table: "Bookings");
        }
    }
}
