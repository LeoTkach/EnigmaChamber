USE EnigmaChamber;
GO

SET IDENTITY_INSERT dbo.Rooms ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms)
BEGIN
    INSERT INTO dbo.Rooms (Id, Name, Description, MinPlayers, MaxPlayers, DurationMinutes, Difficulty, Price, MinAge, HasActor, IsActive) VALUES
    (1, N'Laboratory #7', N'The missing professor left behind encrypted clues.', 2, 6, 60, 3, 120, 12, 0, 1),
    (2, N'Pirate''s Cabin', N'Find the chest key before the storm ends.', 2, 5, 75, 2, 100, 8, 0, 1),
    (3, N'Lost Archive', N'Uncover the mystery of the ancient vault.', 3, 4, 90, 5, 180, 16, 1, 1);
END
SET IDENTITY_INSERT dbo.Rooms OFF;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Staff)
BEGIN
    INSERT INTO dbo.Staff (Name, Role, IsActive) VALUES
    (N'Alexander Smith', N'GameMaster', 1),
    (N'Maria Johnson', N'GameMaster', 1),
    (N'David Bond', N'GameMaster', 1),
    (N'Igor Fox', N'Actor', 1),
    (N'Elena Miller', N'Actor', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Bookings)
BEGIN
    INSERT INTO dbo.Bookings (RoomId, CustomerName, CustomerPhone, PlayerCount, ScheduledAt, Status) VALUES
    (1, N'Orion Team', N'+380501112233', 4, DATEADD(hour, 15, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), N'Pending'),
    (2, N'Kovalenko Family', N'+380671234567', 5, DATEADD(hour, 18, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), N'Pending'),
    (1, N'Students Group', N'+380931112233', 3, DATEADD(day, -1, DATEADD(hour, 15, CAST(CAST(GETDATE() AS DATE) AS DATETIME2))), N'Completed');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RunResults)
BEGIN
    -- FinalMinutes не вказуємо: його порахує тригер trg_RunResults_SetFinalTime
    INSERT INTO dbo.RunResults (BookingId, Success, ElapsedMinutes, HintsUsed, Notes)
    SELECT b.Id, 1, 58, 2, N'Escaped at the last second'
    FROM dbo.Bookings b
    WHERE b.CustomerName = N'Students Group';
END
GO
