USE EnigmaChamber;
GO

-- Тригер 1: заборона перетину бронювань (з урахуванням буфера 15 хв)
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
GO

-- Тригер 2: аудит усіх змін бронювань у таблицю AuditLog
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
GO

-- Тригер 3: автоматичний розрахунок фінального часу через fn_FinalTime
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
GO
