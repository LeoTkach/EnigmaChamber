USE EnigmaChamber;
GO

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
GO

CREATE OR ALTER FUNCTION dbo.fn_FreeSlots
(
    @RoomId INT,
    @Day    DATE
)
RETURNS TABLE
AS
RETURN
(
    WITH Slots AS (
        SELECT CAST(@Day AS DATETIME2) AS SlotStart
        UNION ALL
        SELECT DATEADD(MINUTE, 30, SlotStart)
        FROM Slots
        WHERE SlotStart < DATEADD(HOUR, 22, CAST(@Day AS DATETIME2))
    )
    SELECT s.SlotStart
    FROM Slots s
    INNER JOIN dbo.Rooms r ON r.Id = @RoomId
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Bookings b
        WHERE b.RoomId = @RoomId
          AND b.Status IN (N'Pending', N'InProgress')
          AND s.SlotStart < DATEADD(MINUTE, r.DurationMinutes, b.ScheduledAt)
          AND DATEADD(MINUTE, r.DurationMinutes, s.SlotStart) > b.ScheduledAt
    )
);
GO
