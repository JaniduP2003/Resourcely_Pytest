-- Create Buildings table
CREATE TABLE Buildings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create Floors table
CREATE TABLE Floors (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    BuildingId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (BuildingId) REFERENCES Buildings(Id) ON DELETE CASCADE
);

-- Create Blocks table
CREATE TABLE Blocks (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    FloorId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (FloorId) REFERENCES Floors(Id) ON DELETE CASCADE
);

-- Create Resources table
CREATE TABLE Resources (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Type NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    Capacity INT NOT NULL,
    BlockId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (BlockId) REFERENCES Blocks(Id) ON DELETE CASCADE
);

-- Update Bookings table to use ResourceId instead of Location
ALTER TABLE Bookings DROP COLUMN Location;
ALTER TABLE Bookings ADD ResourceId INT NOT NULL;
ALTER TABLE Bookings ADD EndAt DATETIME2 NOT NULL DEFAULT GETUTCDATE();
ALTER TABLE Bookings ADD CONSTRAINT FK_Bookings_Resources FOREIGN KEY (ResourceId) REFERENCES Resources(Id) ON DELETE CASCADE;

-- Insert sample data
INSERT INTO Buildings (Name, Description) VALUES 
('Main Building', 'Primary academic building'),
('Science Complex', 'Science and research facilities'),
('Engineering Tower', 'Engineering departments and labs');

INSERT INTO Floors (Name, Description, BuildingId) VALUES 
('Ground Floor', 'Main entrance and reception', 1),
('First Floor', 'Administrative offices', 1),
('Second Floor', 'Lecture halls and classrooms', 1),
('Ground Floor', 'Laboratories', 2),
('First Floor', 'Research facilities', 2),
('Ground Floor', 'Engineering workshops', 3),
('Fifth Floor', 'Computer labs', 3);

INSERT INTO Blocks (Name, Description, FloorId) VALUES 
('Block A', 'West wing classrooms', 3),
('Block B', 'East wing lecture halls', 3),
('Lab Block', 'Chemistry and Physics labs', 4),
('Research Block', 'Advanced research facilities', 5),
('Workshop Block', 'Mechanical workshops', 6),
('Computing Block', 'Computer labs and IT facilities', 7);

INSERT INTO Resources (Name, Type, Description, Capacity, BlockId) VALUES 
('Room A101', 'Classroom', 'Standard classroom with projector', 30, 1),
('Room A102', 'Classroom', 'Smart classroom with interactive board', 35, 1),
('Lecture Hall B201', 'Auditorium', 'Large lecture hall with AV equipment', 150, 2),
('Lecture Hall B202', 'Auditorium', 'Medium lecture hall', 100, 2),
('Chemistry Lab C301', 'Laboratory', 'Fully equipped chemistry laboratory', 25, 3),
('Physics Lab C302', 'Laboratory', 'Physics lab with experimental setup', 20, 3),
('Research Lab R401', 'Research Facility', 'Advanced research laboratory', 15, 4),
('Conference Room R402', 'Meeting Room', 'Research team meeting room', 12, 4),
('Workshop W501', 'Workshop', 'Mechanical engineering workshop', 40, 5),
('Computer Lab CL601', 'Computer Lab', 'Software development lab', 30, 6),
('Computer Lab CL602', 'Computer Lab', 'Network and systems lab', 25, 6);