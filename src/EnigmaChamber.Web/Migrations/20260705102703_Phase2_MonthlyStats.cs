using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnigmaChamber.Web.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_MonthlyStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staff_ActorId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Staff_GameMasterId",
                table: "Bookings");

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

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.sp_MonthlyStats
    @Year INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Temporary table to hold the results from the cursor
    CREATE TABLE #StatsTemp
    (
        RoomName NVARCHAR(100),
        GamesTotal INT,
        GamesSuccess INT,
        SuccessRate DECIMAL(5,2),
        AvgFinalMinutes DECIMAL(18,2),
        AvgHints DECIMAL(18,2),
        Revenue DECIMAL(18,2)
    );

    DECLARE @RoomId INT;
    DECLARE @RoomName NVARCHAR(100);
    DECLARE @Price DECIMAL(18,2);

    DECLARE @TotalGames INT;
    DECLARE @SuccessCount INT;
    DECLARE @AvgElapsed DECIMAL(18,2);
    DECLARE @AvgHints DECIMAL(18,2);

    -- Declare the cursor
    DECLARE room_cursor CURSOR FOR
    SELECT Id, Name, Price
    FROM Rooms;

    OPEN room_cursor;
    FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName, @Price;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Calculate stats for the current room
        SELECT 
            @TotalGames = COUNT(*),
            @SuccessCount = SUM(CASE WHEN rr.Success = 1 THEN 1 ELSE 0 END),
            @AvgElapsed = AVG(CAST(rr.ElapsedMinutes AS DECIMAL(18,2))),
            @AvgHints = AVG(CAST(rr.HintsUsed AS DECIMAL(18,2)))
        FROM RunResults rr
        JOIN Bookings b ON rr.BookingId = b.Id
        WHERE b.RoomId = @RoomId
          AND YEAR(b.ScheduledAt) = @Year
          AND MONTH(b.ScheduledAt) = @Month;

        -- Handle NULLs
        SET @TotalGames = ISNULL(@TotalGames, 0);
        SET @SuccessCount = ISNULL(@SuccessCount, 0);

        DECLARE @SuccessRate DECIMAL(5,2) = 0;
        IF @TotalGames > 0
        BEGIN
            SET @SuccessRate = CAST(@SuccessCount AS DECIMAL(5,2)) / @TotalGames * 100.0;
        END

        DECLARE @Revenue DECIMAL(18,2) = @TotalGames * ISNULL(@Price, 0);

        INSERT INTO #StatsTemp (RoomName, GamesTotal, GamesSuccess, SuccessRate, AvgFinalMinutes, AvgHints, Revenue)
        VALUES (@RoomName, @TotalGames, @SuccessCount, @SuccessRate, ISNULL(@AvgElapsed, 0), ISNULL(@AvgHints, 0), @Revenue);

        FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName, @Price;
    END;

    CLOSE room_cursor;
    DEALLOCATE room_cursor;

    SELECT * FROM #StatsTemp ORDER BY RoomName;
    DROP TABLE #StatsTemp;
END
            ");
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

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staff_ActorId",
                table: "Bookings",
                column: "ActorId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Staff_GameMasterId",
                table: "Bookings",
                column: "GameMasterId",
                principalTable: "Staff",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MonthlyStats;");
        }
    }
}
