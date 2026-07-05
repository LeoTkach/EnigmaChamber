USE EnigmaChamber;
GO

-- Курсорна процедура: місячна статистика по кожній кімнаті.
-- Курсор перебирає кімнати, для кожної рахує кількість ігор,
-- відсоток успіху, середній фінальний час, середню кількість підказок і виручку.
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
GO
