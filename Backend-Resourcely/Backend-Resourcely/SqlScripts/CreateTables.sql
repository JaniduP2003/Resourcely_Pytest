-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Email] NVARCHAR(255) NOT NULL UNIQUE,
        [Username] NVARCHAR(255) NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NOT NULL,
        [PasswordSalt] NVARCHAR(MAX) NOT NULL,
        [Role] NVARCHAR(50) NOT NULL DEFAULT 'User',
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        PRIMARY KEY ([Id])
    );
END

-- Create Buildings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Buildings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Buildings] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        [IsActive] BIT NOT NULL DEFAULT 1,
        PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    -- Check if Address column exists and rename it to Description
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Buildings]') AND name = 'Address')
    AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Buildings]') AND name = 'Description')
    BEGIN
        EXEC sp_rename '[Buildings].[Address]', 'Description', 'COLUMN';
    END
END

-- Create Floors table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Floors]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Floors] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Name] NVARCHAR(255) NOT NULL,
        [BuildingId] INT NOT NULL,
        [FloorNumber] INT NOT NULL,
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        [IsActive] BIT NOT NULL DEFAULT 1,
        PRIMARY KEY ([Id]),
        FOREIGN KEY ([BuildingId]) REFERENCES [Buildings]([Id])
    );
END

-- Ensure Floors.Description exists (align with EF model)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('[dbo].[Floors]') AND name = 'Description'
)
BEGIN
    ALTER TABLE [dbo].[Floors]
    ADD [Description] NVARCHAR(500) NOT NULL CONSTRAINT DF_Floors_Description DEFAULT N'';
END

-- Create Blocks table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Blocks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Blocks] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Name] NVARCHAR(255) NOT NULL,
        [FloorId] INT NOT NULL,
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        [IsActive] BIT NOT NULL DEFAULT 1,
        PRIMARY KEY ([Id]),
        FOREIGN KEY ([FloorId]) REFERENCES [Floors]([Id])
    );
END

-- Ensure Blocks.Description exists (align with EF model)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('[dbo].[Blocks]') AND name = 'Description'
)
BEGIN
    ALTER TABLE [dbo].[Blocks]
    ADD [Description] NVARCHAR(500) NOT NULL CONSTRAINT DF_Blocks_Description DEFAULT N'';
END

-- Create Resources table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Resources]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Resources] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Name] NVARCHAR(255) NOT NULL,
        [Type] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [Capacity] INT NOT NULL,
        [BlockId] INT NOT NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsRestricted] BIT NOT NULL DEFAULT 0,
        [RestrictedToRoles] NVARCHAR(500) NULL,
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        PRIMARY KEY ([Id]),
        FOREIGN KEY ([BlockId]) REFERENCES [Blocks]([Id])
    );
END

-- Create Bookings table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Bookings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Bookings] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [ResourceId] INT NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [BookingAt] DATETIME2(6) NOT NULL,
        [EndAt] DATETIME2(6) NOT NULL,
        [Reason] NVARCHAR(500) NOT NULL,
        [Capacity] INT NOT NULL,
        [Contact] NVARCHAR(255) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [ApprovedBy] NVARCHAR(255) NULL,
        [ApprovedAt] DATETIME2(6) NULL,
        [RejectionReason] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        PRIMARY KEY ([Id]),
        FOREIGN KEY ([ResourceId]) REFERENCES [Resources]([Id])
    );
END

-- Insert sample data for testing
-- Sample Buildings
IF NOT EXISTS (SELECT * FROM [Buildings])
BEGIN
    INSERT INTO [Buildings] ([Name], [Description]) VALUES
    ('Main Building', 'Main university building with administrative offices'),
    ('Science Building', 'Building dedicated to science and research'),
    ('Library Building', 'Central library with reading halls and study areas');
END

-- Sample Floors
IF NOT EXISTS (SELECT * FROM [Floors])
BEGIN
    INSERT INTO [Floors] ([Name], [BuildingId], [FloorNumber]) VALUES
    ('Ground Floor', 1, 0),
    ('First Floor', 1, 1),
    ('Second Floor', 1, 2),
    ('Ground Floor', 2, 0),
    ('First Floor', 2, 1),
    ('Ground Floor', 3, 0);
END

-- Sample Blocks
IF NOT EXISTS (SELECT * FROM [Blocks])
BEGIN
    INSERT INTO [Blocks] ([Name], [FloorId]) VALUES
    ('Block A', 1),
    ('Block B', 1),
    ('Block C', 2),
    ('Block D', 2),
    ('Lab Block', 4),
    ('Reading Hall', 6);
END

-- Sample Resources
IF NOT EXISTS (SELECT * FROM [Resources])
BEGIN
    INSERT INTO [Resources] ([Name], [Type], [Description], [Capacity], [BlockId], [IsRestricted], [RestrictedToRoles]) VALUES
    ('Conference Room A', 'Meeting Room', 'Large conference room with projector', 20, 1, 0, ''),
    ('Lab 101', 'Lab', 'Computer Science Lab', 30, 5, 1, 'Student,Lecturer'),
    ('Study Hall', 'Study Room', 'Quiet study area', 50, 6, 0, ''),
    ('Meeting Room B', 'Meeting Room', 'Small meeting room', 8, 2, 1, 'Staff,Lecturer'),
    ('Auditorium', 'Auditorium', 'Main auditorium for events', 200, 3, 1, 'Admin,Lecturer');
END

-- Sample Admin User (password is "admin123")
IF NOT EXISTS (SELECT * FROM [Users] WHERE [Email] = 'admin@resourcely.com')
BEGIN
    INSERT INTO [Users] ([Email], [Username], [PasswordHash], [PasswordSalt], [Role]) VALUES
    ('admin@resourcely.com', 'admin', 
     'AQAAAAIAAYagAAAAENxF8xY8yKzQ1J8F9v9F8F9F8F9F8F9F8F9F8F9F8F9F8F9F8F9F8F9F8F9F8F9F8F9F8Q==', 
     'salt123', 'Admin');
END


/* ===========================
   Ensure Users.Username is unique (handy)
=========================== */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('[dbo].[Users]')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [dbo].[Users]([Username]);
END
GO

/* ===========================
   Departments table (idempotent)
=========================== */
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Departments]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[Departments] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_Departments_Name] ON [dbo].[Departments]([Name]);
END
GO

/* ===========================
   LecturerProfiles table (1:1 with Users; many-to-1 to Departments)
=========================== */
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LecturerProfiles]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[LecturerProfiles] (
        [UserId] INT NOT NULL,        -- PK = FK to Users.Id
        [DepartmentId] INT NOT NULL,
        CONSTRAINT [PK_LecturerProfiles] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_LecturerProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LecturerProfiles_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [dbo].[Departments]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_LecturerProfiles_DepartmentId] ON [dbo].[LecturerProfiles]([DepartmentId]);
END
GO

/* ===========================
   Create Batches
=========================== */
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Batches]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[Batches] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Name] NVARCHAR(200) NOT NULL,
        [Code] NVARCHAR(50) NULL,
        [StartDate] DATE NULL,
        [EndDate] DATE NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_Batches] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Batches_Name] UNIQUE ([Name])
    );
END
GO

/* ===========================
   Create StudentProfiles (1:1 with Users; many-to-1 to Batches)
=========================== */
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StudentProfiles]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[StudentProfiles] (
        [UserId] INT NOT NULL,        -- PK = FK to Users.Id
        [BatchId] INT NOT NULL,
        [CreatedAt] DATETIME2(6) NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT [PK_StudentProfiles] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_StudentProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StudentProfiles_Batches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [dbo].[Batches]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_StudentProfiles_BatchId] ON [dbo].[StudentProfiles]([BatchId]);
END
GO

/* ===========================
   Guardrail: Only Role='Student' may have a StudentProfile
   (Cross-table CHECKs aren't supported; use a trigger)
=========================== */
IF OBJECT_ID(N'[dbo].[TRG_StudentProfiles_OnlyStudents]', N'TR') IS NULL
BEGIN
    EXEC('
    CREATE TRIGGER [dbo].[TRG_StudentProfiles_OnlyStudents]
    ON [dbo].[StudentProfiles]
    AFTER INSERT, UPDATE
    AS
    BEGIN
        SET NOCOUNT ON;

        IF EXISTS (
            SELECT 1
            FROM inserted i
            JOIN [dbo].[Users] u ON u.[Id] = i.[UserId]
            WHERE UPPER(u.[Role]) <> ''STUDENT''
        )
        BEGIN
            RAISERROR (''Only users with Role=Student can have a StudentProfile.'', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END
    END
    ');
END
GO

/* ===========================
   Fix Bookings.UserId type (NVARCHAR -> INT) only if safe
   Then add FK Bookings(UserId) -> Users(Id)
=========================== */
-- Add a quick helper: does Bookings.UserId exist and is NVARCHAR?
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Bookings]') AND name = 'UserId')
AND EXISTS (
    SELECT 1 FROM sys.types t
    JOIN sys.columns c ON c.user_type_id = t.user_type_id AND c.system_type_id = t.system_type_id
    WHERE c.object_id = OBJECT_ID('[dbo].[Bookings]')
      AND c.name = 'UserId'
      AND t.name IN ('nvarchar','varchar','nchar','char')
)
BEGIN
    -- Only alter if table is empty to avoid conversion problems
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Bookings])
    BEGIN
        -- Remove any existing FK named like FK_*_UserId (defensive)
        DECLARE @dropSql NVARCHAR(MAX) = N'';
        SELECT @dropSql = @dropSql + N'ALTER TABLE [dbo].[Bookings] DROP CONSTRAINT [' + fk.name + '];' + CHAR(10)
        FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID('[dbo].[Bookings]') AND fk.name LIKE 'FK%UserId%';

        IF LEN(@dropSql) > 0 EXEC sp_executesql @dropSql;

        -- Change column type
        ALTER TABLE [dbo].[Bookings] ALTER COLUMN [UserId] INT NOT NULL;

        -- Add proper FK
        ALTER TABLE [dbo].[Bookings]
        ADD CONSTRAINT [FK_Bookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
    END
END
ELSE
BEGIN
    -- If it's already INT and FK missing, add FK
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Bookings]') AND name = 'UserId' AND system_type_id = TYPE_ID('int'))
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bookings_Users_UserId')
    BEGIN
        ALTER TABLE [dbo].[Bookings]
        ADD CONSTRAINT [FK_Bookings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
    END
END
GO

/* ===========================
   Seed: Batches (only if empty)
=========================== */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Batches])
BEGIN
    INSERT INTO [dbo].[Batches] ([Name], [Code], [StartDate])
    VALUES
      (N'2025 – CS', N'CS25', '2025-01-01'),
      (N'2025 – IT', N'IT25', '2025-01-01');
END
GO

/* ===========================
   Seed: Student user + profile (demo)
=========================== */
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'student1@resourcely.com')
BEGIN
    INSERT INTO [dbo].[Users] ([Email], [Username], [PasswordHash], [PasswordSalt], [Role])
    VALUES (N'student1@resourcely.com', N'student1',
            N'REPLACE_WITH_HASH', N'salt123', N'Student');

    DECLARE @StudentId INT = SCOPE_IDENTITY();
    DECLARE @BatchId   INT = (SELECT TOP 1 [Id] FROM [dbo].[Batches] WHERE [Name] = N'2025 – CS' ORDER BY [Id]);

    INSERT INTO [dbo].[StudentProfiles] ([UserId], [BatchId]) VALUES (@StudentId, @BatchId);
END
GO


-- Only if Bookings empty
IF NOT EXISTS (SELECT 1 FROM [dbo].[Bookings])
BEGIN
    ALTER TABLE [dbo].[Bookings] ALTER COLUMN [UserId] INT NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bookings_Users_UserId')
    BEGIN
        ALTER TABLE [dbo].[Bookings]
        ADD CONSTRAINT [FK_Bookings_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;
    END
END


-- 1) Add a new INT column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[Bookings]') AND name = 'UserIdInt')
BEGIN
    ALTER TABLE [dbo].[Bookings] ADD [UserIdInt] INT NULL;
END

-- 2) Try to convert/copy values (will fail if non-numeric values exist)
UPDATE b
SET b.UserIdInt = TRY_CONVERT(INT, b.UserId)
FROM [dbo].[Bookings] b;

-- 3) Check any rows failed to convert
IF EXISTS (SELECT 1 FROM [dbo].[Bookings] WHERE [UserIdInt] IS NULL)
BEGIN
    RAISERROR('Some Bookings.UserId values are not numeric and cannot be converted to INT.',16,1);
    RETURN;
END

-- 4) Make new column NOT NULL
ALTER TABLE [dbo].[Bookings] ALTER COLUMN [UserIdInt] INT NOT NULL;

-- 5) Drop any existing FK on old UserId
DECLARE @dropSql NVARCHAR(MAX) = N'';
SELECT @dropSql = @dropSql + N'ALTER TABLE [dbo].[Bookings] DROP CONSTRAINT [' + fk.name + '];' + CHAR(10)
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('[dbo].[Bookings]') AND fk.name LIKE 'FK%UserId%';
IF LEN(@dropSql) > 0 EXEC sp_executesql @dropSql;

-- 6) Drop old column and rename new one
ALTER TABLE [dbo].[Bookings] DROP COLUMN [UserId];
EXEC sp_rename '[dbo].[Bookings].[UserIdInt]', 'UserId', 'COLUMN';

-- 7) Add proper FK
ALTER TABLE [dbo].[Bookings]
ADD CONSTRAINT [FK_Bookings_Users_UserId]
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION;

