-- Manual SQL script to update admin password  
-- Run this against your SASMS database

USE SASMSDb; -- Adjust database name if different
GO

-- Update admin user password with correct BCrypt hash for "Admin@123456"
UPDATE Users
SET Password = '$2a$12$exosm0CFgW7RAB9GfeIVH.SykedAUY33HdRkO1K8sJ4OEbbWAt2jO'
WHERE Email = 'admin@sasms.edu';

-- Verify the update
SELECT Id, Email, Role, IsActive, Password
FROM Users
WHERE Email = 'admin@sasms.edu';
GO
