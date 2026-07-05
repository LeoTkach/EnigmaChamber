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

    -- Build a JSON summary of changes (simplistic for demo)
    -- In a real scenario we might compare DELETED and INSERTED
    DECLARE @Details NVARCHAR(MAX) = '';
    
    IF @Action = 'UPDATE'
    BEGIN
        SELECT TOP 1 @Details = CONCAT('Status changed to ', i.Status)
        FROM inserted i
        JOIN deleted d ON i.Id = d.Id
        WHERE i.Status != d.Status;
    END
    ELSE IF @Action = 'INSERT'
    BEGIN
        SELECT TOP 1 @Details = CONCAT('Booking created for ', i.CustomerName)
        FROM inserted i;
    END
    ELSE IF @Action = 'DELETE'
    BEGIN
        SELECT TOP 1 @Details = CONCAT('Booking deleted for ', d.CustomerName)
        FROM deleted d;
    END

    INSERT INTO AuditLog (TableName, RecordId, Action, NewValue)
    SELECT 
        'Bookings',
        COALESCE(i.Id, d.Id),
        @Action,
        @Details
    FROM inserted i
    FULL OUTER JOIN deleted d ON i.Id = d.Id;
END
