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

        -- 1. Get room duration and config
        DECLARE @Duration INT;
        SELECT @Duration = DurationMinutes FROM Rooms WHERE Id = @RoomId;

        DECLARE @Buffer INT = 15;
        DECLARE @TotalDuration INT = @Duration + @Buffer;

        DECLARE @EndTime DATETIME = DATEADD(minute, @TotalDuration, @ScheduledAt);

        -- 2. Check for overlapping bookings
        IF EXISTS (
            SELECT 1 FROM Bookings 
            WHERE RoomId = @RoomId 
            AND Status != 'Cancelled'
            -- Existing booking overlaps if it starts before new booking ends AND ends after new booking starts
            AND ScheduledAt < @EndTime
            AND DATEADD(minute, @TotalDuration, ScheduledAt) > @ScheduledAt
        )
        BEGIN
            -- Throw exception if overlaps
            THROW 50001, 'Time is already booked.', 1;
        END

        -- 3. Insert booking
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
