-- Update Resources table to add permission/restriction fields
ALTER TABLE Resources
ADD IsRestricted BIT NOT NULL DEFAULT 0,
    RestrictedToRoles NVARCHAR(500) NOT NULL DEFAULT '';

-- Update Bookings table to add approval/status fields
ALTER TABLE Bookings
ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    ApprovedBy NVARCHAR(255) NULL,
    ApprovedAt DATETIME2 NULL,
    RejectionReason NVARCHAR(1000) NULL;

-- Add indexes for better performance
CREATE INDEX IX_Bookings_Status ON Bookings (Status);
CREATE INDEX IX_Bookings_UserId ON Bookings (UserId);
CREATE INDEX IX_Resources_IsRestricted ON Resources (IsRestricted);

-- Update existing bookings to have 'Approved' status (assuming they were previously auto-approved)
UPDATE Bookings SET Status = 'Approved' WHERE Status = 'Pending';