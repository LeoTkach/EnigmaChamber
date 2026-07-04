USE EnigmaChamber;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateBooking
    @RoomId        INT,
    @CustomerName  NVARCHAR(100),
    @CustomerPhone NVARCHAR(20),
    @PlayerCount   INT,
    @ScheduledAt   DATETIME2,
    @NewBookingId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Rooms WHERE Id = @RoomId AND IsActive = 1)
        THROW 50001, N'Кімната не знайдена або неактивна.', 1;

    DECLARE @MaxPlayers INT;
    SELECT @MaxPlayers = MaxPlayers FROM dbo.Rooms WHERE Id = @RoomId;

    IF @PlayerCount > @MaxPlayers
        THROW 50002, N'Забагато гравців для цієї кімнати.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.Bookings b
        INNER JOIN dbo.Rooms r ON r.Id = b.RoomId
        WHERE b.RoomId = @RoomId
          AND b.Status IN (N'Pending', N'InProgress')
          AND b.ScheduledAt < DATEADD(MINUTE, r.DurationMinutes, @ScheduledAt)
          AND DATEADD(MINUTE, r.DurationMinutes, b.ScheduledAt) > @ScheduledAt
    )
        THROW 50003, N'Цей часовий слот уже зайнятий.', 1;

    INSERT INTO dbo.Bookings (RoomId, CustomerName, CustomerPhone, PlayerCount, ScheduledAt, Status)
    VALUES (@RoomId, @CustomerName, @CustomerPhone, @PlayerCount, @ScheduledAt, N'Pending');

    SET @NewBookingId = SCOPE_IDENTITY();
END
GO
