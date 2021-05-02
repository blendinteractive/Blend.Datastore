-- 0000.sql
-- The initial database setup.
CREATE TABLE Person (
	Id int NOT NULL IDENTITY(1, 1),
	Email nvarchar(128) NOT NULL,
	FullName nvarchar(128) NOT NULL,

	CONSTRAINT PK_Person PRIMARY KEY (Id)
);

-- You can separate commands using GO. They will be executed in separate batches.
-- The GO command must be the only command on that line, though the system will tolerate
-- whitespace, semicolons, and inline comments.
GO -- Multi-line comments will break stuff.

-- Fill in with some data
INSERT INTO Person (Email, FullName) VALUES ('neo@thematrix.com', 'Neo');
INSERT INTO Person (Email, FullName) VALUES ('thor@asgard.net', 'Thor');
INSERT INTO Person (Email, FullName) VALUES ('kal-el@dailyplanet.com', 'Man O Steel');

-- You MUST create/alter your stored procedure in every batch.
-- For the first batch, you must create it.
GO

CREATE PROCEDURE DatabaseVersion AS 
BEGIN 
	SELECT 1
END