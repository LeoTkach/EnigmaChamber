USE EnigmaChamber;
GO

CREATE OR ALTER PROCEDURE dbo.sp_MonthlyRoomStats
    @Year  INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RoomId   INT;
    DECLARE @RoomName NVARCHAR(100);
    DECLARE @Total    INT;
    DECLARE @Wins     INT;
    DECLARE @AvgFinal DECIMAL(10,2);

    DECLARE room_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT Id, Name FROM dbo.Rooms WHERE IsActive = 1;

    OPEN room_cursor;
    FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT
            @Total = COUNT(*),
            @Wins = SUM(CASE WHEN rr.Success = 1 THEN 1 ELSE 0 END),
            @AvgFinal = AVG(CAST(rr.FinalMinutes AS DECIMAL(10,2)))
        FROM dbo.Bookings b
        LEFT JOIN dbo.RunResults rr ON rr.BookingId = b.Id
        WHERE b.RoomId = @RoomId
          AND b.Status = N'Completed'
          AND YEAR(b.ScheduledAt) = @Year
          AND MONTH(b.ScheduledAt) = @Month;

        PRINT CONCAT(
            N'Кімната: ', @RoomName,
            N' | Ігор: ', ISNULL(@Total, 0),
            N' | Успіх: ', ISNULL(@Wins, 0),
            N' | Середній час: ', ISNULL(@AvgFinal, 0), N' хв'
        );

        FETCH NEXT FROM room_cursor INTO @RoomId, @RoomName;
    END

    CLOSE room_cursor;
    DEALLOCATE room_cursor;
END
GO
