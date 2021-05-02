-- This "migration" actually deletes the table and stored procedure.
-- You would normally not do this. This is only for automated testing.

DROP TABLE Person;

GO

DROP PROCEDURE DatabaseVersion;
