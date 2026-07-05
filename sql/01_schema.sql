-- EnigmaChamber: core schema (reference copy — the real schema is created by EF Core migrations)
USE EnigmaChamber;
GO

IF OBJECT_ID('dbo.Rooms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Rooms (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY,
        Name            NVARCHAR(100)     NOT NULL,
        Description     NVARCHAR(500)     NULL,
        MinPlayers      INT               NOT NULL CONSTRAINT DF_Rooms_MinPlayers DEFAULT (2),
        MaxPlayers      INT               NOT NULL CONSTRAINT CK_Rooms_MaxPlayers CHECK (MaxPlayers BETWEEN 2 AND 12),
        DurationMinutes INT               NOT NULL CONSTRAINT CK_Rooms_Duration CHECK (DurationMinutes BETWEEN 30 AND 120),
        Difficulty      INT               NOT NULL CONSTRAINT DF_Rooms_Difficulty DEFAULT (3),
        Price           DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Rooms_Price DEFAULT (0),
        MinAge          INT               NOT NULL CONSTRAINT DF_Rooms_MinAge DEFAULT (0),
        HasActor        BIT               NOT NULL CONSTRAINT DF_Rooms_HasActor DEFAULT (0),
        IsActive        BIT               NOT NULL CONSTRAINT DF_Rooms_IsActive DEFAULT (1),
        CreatedAt       DATETIME2         NOT NULL CONSTRAINT DF_Rooms_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.Staff', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Staff (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Staff PRIMARY KEY,
        Name      NVARCHAR(100)     NOT NULL,
        Role      NVARCHAR(50)      NOT NULL CONSTRAINT CK_Staff_Role CHECK (Role IN (N'GameMaster', N'Actor')),
        IsActive  BIT               NOT NULL CONSTRAINT DF_Staff_IsActive DEFAULT (1),
        CreatedAt DATETIME2         NOT NULL CONSTRAINT DF_Staff_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.Bookings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bookings (
        Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
        RoomId        INT               NOT NULL,
        CustomerName  NVARCHAR(100)     NOT NULL,
        CustomerPhone NVARCHAR(20)      NOT NULL,
        PlayerCount   INT               NOT NULL CONSTRAINT CK_Bookings_PlayerCount CHECK (PlayerCount >= 1),
        ScheduledAt   DATETIME2         NOT NULL,
        Status        NVARCHAR(20)      NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT (N'Pending'),
        GameMasterId  INT               NULL,
        ActorId       INT               NULL,
        CreatedAt     DATETIME2         NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Bookings_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(Id),
        CONSTRAINT FK_Bookings_Staff_GM FOREIGN KEY (GameMasterId) REFERENCES dbo.Staff(Id),
        CONSTRAINT FK_Bookings_Staff_Actor FOREIGN KEY (ActorId) REFERENCES dbo.Staff(Id),
        CONSTRAINT CK_Bookings_Status CHECK (Status IN (N'Pending', N'InProgress', N'Completed', N'Cancelled', N'Failed'))
    );
    CREATE INDEX IX_Bookings_Room_Scheduled ON dbo.Bookings(RoomId, ScheduledAt);
END
GO

IF OBJECT_ID('dbo.RunResults', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RunResults (
        Id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RunResults PRIMARY KEY,
        BookingId      INT               NOT NULL,
        Success        BIT               NOT NULL,
        ElapsedMinutes INT               NOT NULL CONSTRAINT CK_RunResults_Elapsed CHECK (ElapsedMinutes >= 0),
        HintsUsed      INT               NOT NULL CONSTRAINT DF_RunResults_Hints DEFAULT (0),
        FinalMinutes   INT               NULL,
        Notes          NVARCHAR(500)     NULL,
        CompletedAt    DATETIME2         NOT NULL CONSTRAINT DF_RunResults_CompletedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_RunResults_Bookings FOREIGN KEY (BookingId) REFERENCES dbo.Bookings(Id),
        CONSTRAINT UQ_RunResults_Booking UNIQUE (BookingId)
    );
END
GO

IF OBJECT_ID('dbo.AuditLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog (
        Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
        TableName  NVARCHAR(64)         NOT NULL,
        RecordId   INT                  NOT NULL,
        Action     NVARCHAR(32)         NOT NULL,
        OldValue   NVARCHAR(MAX)        NULL,
        NewValue   NVARCHAR(MAX)        NULL,
        ChangedAt  DATETIME2            NOT NULL CONSTRAINT DF_AuditLog_ChangedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO
