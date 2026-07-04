USE EnigmaChamber;
GO

CREATE OR ALTER TRIGGER dbo.trg_Bookings_NoOverlap
ON dbo.Bookings
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN dbo.Bookings b ON b.Id <> i.Id AND b.RoomId = i.RoomId
        INNER JOIN dbo.Rooms r ON r.Id = i.RoomId
        WHERE i.Status IN (N'Pending', N'InProgress')
          AND b.Status IN (N'Pending', N'InProgress')
          AND i.ScheduledAt < DATEADD(MINUTE, r.DurationMinutes, b.ScheduledAt)
          AND DATEADD(MINUTE, r.DurationMinutes, i.ScheduledAt) > b.ScheduledAt
    )
    BEGIN
        RAISERROR (N'Подвійне бронювання заборонено тригером.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END
GO

CREATE OR ALTER TRIGGER dbo.trg_Bookings_Audit
ON dbo.Bookings
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLog (TableName, RecordId, Action, OldValue, NewValue)
    SELECT
        N'Bookings',
        i.Id,
        N'UPDATE',
        CONCAT(N'Status=', d.Status, N'; ScheduledAt=', CONVERT(NVARCHAR(30), d.ScheduledAt, 126)),
        CONCAT(N'Status=', i.Status, N'; ScheduledAt=', CONVERT(NVARCHAR(30), i.ScheduledAt, 126))
    FROM inserted i
    INNER JOIN deleted d ON d.Id = i.Id
    WHERE i.Status <> d.Status OR i.ScheduledAt <> d.ScheduledAt;
END
GO

CREATE OR ALTER TRIGGER dbo.trg_RunResults_SetFinalTime
ON dbo.RunResults
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE rr
    SET FinalMinutes = dbo.fn_FinalTime(rr.ElapsedMinutes, rr.HintsUsed)
    FROM dbo.RunResults rr
    INNER JOIN inserted i ON i.Id = rr.Id;
END
GO
