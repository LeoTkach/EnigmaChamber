using System;
using EnigmaChamber.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnigmaChamber.Web.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260705100000_Phase2_ResultsAndStats")]
/// <summary>
/// Прибирає таблицю Puzzles, додає T-SQL об'єкти другої фази:
/// скалярну функцію fn_FinalTime, тригери trg_RunResults_SetFinalTime і
/// trg_Bookings_NoOverlap та курсорну процедуру sp_MonthlyStats.
/// </summary>
public partial class Phase2_ResultsAndStats : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Puzzles");

        // скалярна функція: фінальний час = час гри + штраф 2 хв за кожну підказку
        migrationBuilder.Sql("""
CREATE OR ALTER FUNCTION dbo.fn_FinalTime
(
    @ElapsedMinutes INT,
    @HintsUsed      INT
)
RETURNS INT
AS
BEGIN
    RETURN @ElapsedMinutes + (@HintsUsed * 2);
END
""");

        // тригер: автоматично рахує FinalMinutes через fn_FinalTime
        migrationBuilder.Sql("""
CREATE OR ALTER TRIGGER trg_RunResults_SetFinalTime
ON RunResults
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE rr
    SET FinalMinutes = dbo.fn_FinalTime(rr.ElapsedMinutes, rr.HintsUsed)
    FROM RunResults rr
    JOIN inserted i ON rr.Id = i.Id;
END
""");

        // тригер: заборона перетину бронювань (з урахуванням буфера 15 хв)
        migrationBuilder.Sql("""
CREATE OR ALTER TRIGGER trg_Bookings_NoOverlap
ON Bookings
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Rooms r ON r.Id = i.RoomId
        JOIN Bookings b ON b.RoomId = i.RoomId
            AND b.Id <> i.Id
            AND b.Status <> N'Cancelled'
            AND i.Status <> N'Cancelled'
            AND b.ScheduledAt < DATEADD(MINUTE, r.DurationMinutes + 15, i.ScheduledAt)
            AND DATEADD(MINUTE, r.DurationMinutes + 15, b.ScheduledAt) > i.ScheduledAt
    )
    BEGIN
        THROW 50002, N'Бронювання перетинається з існуючим.', 1;
    END
END
""");

        // виправлення тригера аудиту: деталі рахуються для кожного рядка окремо
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

    INSERT INTO AuditLog (TableName, RecordId, Action, OldValue, NewValue)
    SELECT
        'Bookings',
        COALESCE(i.Id, d.Id),
        @Action,
        CASE WHEN @Action = 'UPDATE' THEN CONCAT('Status=', d.Status) END,
        CASE @Action
            WHEN 'INSERT' THEN CONCAT('Booking created for ', i.CustomerName)
            WHEN 'DELETE' THEN CONCAT('Booking deleted for ', d.CustomerName)
            ELSE CONCAT('Status changed to ', i.Status)
        END
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.Id = d.Id;
END
""");

        // курсорна процедура: місячна статистика по кожній кімнаті
        migrationBuilder.Sql("""
CREATE OR ALTER PROCEDURE dbo.sp_MonthlyStats
    @Year  INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Result TABLE (
        RoomName        NVARCHAR(100),
        GamesTotal      INT,
        GamesSuccess    INT,
        SuccessRate     DECIMAL(18,2),
        AvgFinalMinutes DECIMAL(18,2) NULL,
        AvgHints        DECIMAL(18,2) NULL,
        Revenue         DECIMAL(18,2)
    );

    DECLARE @RoomId INT, @RoomName NVARCHAR(100), @Price DECIMAL(18,2);
    DECLARE @Total INT, @Success INT, @AvgFinal DECIMAL(18,2), @AvgHints DECIMAL(18,2);

    DECLARE room_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id, Name, Price FROM dbo.Rooms;

    OPEN room_cursor;
    FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName, @Price;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT
            @Total    = COUNT(*),
            @Success  = ISNULL(SUM(CASE WHEN rr.Success = 1 THEN 1 ELSE 0 END), 0),
            @AvgFinal = AVG(CAST(rr.FinalMinutes AS DECIMAL(18,2))),
            @AvgHints = AVG(CAST(rr.HintsUsed AS DECIMAL(18,2)))
        FROM dbo.Bookings b
        JOIN dbo.RunResults rr ON rr.BookingId = b.Id
        WHERE b.RoomId = @RoomId
          AND b.Status = N'Completed'
          AND YEAR(b.ScheduledAt) = @Year
          AND MONTH(b.ScheduledAt) = @Month;

        INSERT INTO @Result VALUES (
            @RoomName,
            ISNULL(@Total, 0),
            ISNULL(@Success, 0),
            CASE WHEN ISNULL(@Total, 0) = 0 THEN 0
                 ELSE CAST(@Success AS DECIMAL(18,2)) * 100 / @Total END,
            @AvgFinal,
            @AvgHints,
            ISNULL(@Total, 0) * @Price);

        FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName, @Price;
    END

    CLOSE room_cursor;
    DEALLOCATE room_cursor;

    SELECT RoomName, GamesTotal, GamesSuccess, SuccessRate, AvgFinalMinutes, AvgHints, Revenue
    FROM @Result
    ORDER BY Revenue DESC, RoomName;
END
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MonthlyStats;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_Bookings_NoOverlap;");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_RunResults_SetFinalTime;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_FinalTime;");

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
                table.ForeignKey(
                    name: "FK_Puzzles_Rooms_RoomId",
                    column: x => x.RoomId,
                    principalTable: "Rooms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Puzzles_RoomId", table: "Puzzles", column: "RoomId");
    }
}
