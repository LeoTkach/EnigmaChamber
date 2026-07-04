USE EnigmaChamber;
GO

SET IDENTITY_INSERT dbo.Rooms ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Rooms)
BEGIN
    INSERT INTO dbo.Rooms (Id, Name, Description, MaxPlayers, DurationMinutes, IsActive) VALUES
    (1, N'Лабораторія №7', N'Зниклий професор залишив зашифровані підказки.', 6, 60, 1),
    (2, N'Піратська каюта', N'Знайдіть ключ від скрині до закінчення шторму.', 5, 75, 1),
    (3, N'Втрачений архів', N'Розкрийте таємницю старовинного сховища.', 4, 90, 1);
END
SET IDENTITY_INSERT dbo.Rooms OFF;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Puzzles)
BEGIN
    INSERT INTO dbo.Puzzles (RoomId, Title, HintText, OrderIndex) VALUES
    (1, N'Сейф з кодом', N'Код = рік заснування університету', 1),
    (1, N'Лазерна сітка', N'Рухайтесь по тіні', 2),
    (2, N'Мапа скарбів', N'X позначає діри в каюті', 1),
    (3, N'Шифр Цезаря', N'Зсув на 3', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Bookings)
BEGIN
    INSERT INTO dbo.Bookings (RoomId, CustomerName, CustomerPhone, PlayerCount, ScheduledAt, Status) VALUES
    (1, N'Команда Orion', N'+380501112233', 4, DATEADD(hour, 3, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), N'Pending'),
    (2, N'Сім''я Коваленко', N'+380671234567', 5, DATEADD(hour, 5, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), N'Pending'),
    (1, N'Студенти ПЗПІ', N'+380931112233', 3, DATEADD(day, -1, DATEADD(hour, 15, CAST(CAST(GETDATE() AS DATE) AS DATETIME2))), N'Completed');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RunResults)
BEGIN
    INSERT INTO dbo.RunResults (BookingId, Success, ElapsedMinutes, HintsUsed, FinalMinutes, Notes)
    SELECT b.Id, 1, 58, 2, 62, N'Встигли в останні секунди'
    FROM dbo.Bookings b
    WHERE b.CustomerName = N'Студенти ПЗПІ';
END
GO
