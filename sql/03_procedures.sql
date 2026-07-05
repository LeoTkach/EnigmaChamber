USE EnigmaChamber;
GO

-- Створення бронювання з перевіркою вільного слоту (буфер 15 хв на прибирання)
CREATE OR ALTER PROCEDURE dbo.sp_CreateBooking
    @RoomId        INT,
    @CustomerName  NVARCHAR(100),
    @CustomerPhone NVARCHAR(20),
    @PlayerCount   INT,
    @ScheduledAt   DATETIME,
    @BookingId     INT OUTPUT
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
GO
