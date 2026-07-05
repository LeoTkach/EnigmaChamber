CREATE OR ALTER PROCEDURE dbo.sp_MonthlyStats
    @Year INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Temporary table to hold the results from the cursor
    CREATE TABLE #StatsTemp
    (
        RoomId INT,
        RoomName NVARCHAR(100),
        TotalGames INT,
        SuccessCount INT,
        SuccessRate DECIMAL(5,2),
        AvgElapsedMinutes INT,
        AvgHintsUsed DECIMAL(5,2)
    );

    DECLARE @RoomId INT;
    DECLARE @RoomName NVARCHAR(100);

    DECLARE @TotalGames INT;
    DECLARE @SuccessCount INT;
    DECLARE @AvgElapsed INT;
    DECLARE @AvgHints DECIMAL(5,2);

    -- Declare the cursor
    DECLARE room_cursor CURSOR FOR
    SELECT Id, Name
    FROM Rooms
    WHERE IsActive = 1;

    OPEN room_cursor;
    FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Calculate stats for the current room
        SELECT 
            @TotalGames = COUNT(*),
            @SuccessCount = SUM(CASE WHEN rr.Success = 1 THEN 1 ELSE 0 END),
            @AvgElapsed = AVG(rr.ElapsedMinutes),
            @AvgHints = AVG(CAST(rr.HintsUsed AS DECIMAL(5,2)))
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

        INSERT INTO #StatsTemp (RoomId, RoomName, TotalGames, SuccessCount, SuccessRate, AvgElapsedMinutes, AvgHintsUsed)
        VALUES (@RoomId, @RoomName, @TotalGames, @SuccessCount, @SuccessRate, ISNULL(@AvgElapsed, 0), ISNULL(@AvgHints, 0));

        FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName;
    END;

    CLOSE room_cursor;
    DEALLOCATE room_cursor;

    SELECT * FROM #StatsTemp ORDER BY RoomName;
    DROP TABLE #StatsTemp;
END
