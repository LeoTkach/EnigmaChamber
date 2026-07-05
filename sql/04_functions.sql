USE EnigmaChamber;
GO

-- Скалярна функція: фінальний час = час гри + штраф 2 хв за кожну підказку
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

-- Таблична функція: сітка слотів кімнати на діапазон дат
-- (робочі години 10:00–22:00, буфер 15 хв між іграми)
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
GO
