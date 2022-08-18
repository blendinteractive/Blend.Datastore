-- 0001.sql

-- Add the field
ALTER TABLE Person ADD FavoriteColor nvarchar(32) NULL;

GO

-- Add a test person
INSERT INTO Person (Email, FullName, FavoriteColor) VALUES ('batman@wayneindustries.com', 'Bats', 'black');

-- Again, the stored procedure update must be in a separate batch, and must be the
-- first item in that batch.
GO

-- This will always alter the DatabaseVersion (or whatever you all your stored
-- procedure) with the updated version. The version will always be one more than 
-- the file. So this file 0001, the sproc returns 2.
ALTER PROCEDURE DatabaseVersion AS 
BEGIN 
	SELECT 2 
END