# BlendInteractive.Datastore

This is a minimalist SQL migration system and access pattern. It provides rudimentary database migration capabilities, and a relatively simple pattern for executing queries and transactions against a database. `BlendInteractive.Datastore` does not feature any DSL for migrations/queries/etc, but rather executes SQL that you provide.

This is mostly geared for situations where raw SQL is a better choice than expecting a framework to convert a DSL into SQL. Examples might be where it's a small schema for which something like Entity Framework would be overkill, or with tables/procedures/indicies that are finely tuned and have to be built via direct SQL.

## How it works

### Migrations

Each step in your migration path is a SQL file embedded in your project. The files are numbered starting at `0000` (by default). For example, you might have `0000.sql`, `0001.sql`, `0002.sql`, etc. If you need more than 9,999 revisions to your schema... this probably isn't the project for you, but you can override the `SqlFileNamePattern` property to get more digits.

Each file must be an embedded resource. Each SQL file will create or alter a stored procedure that returns the current version of the database.

For example:

```sql
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
```

Then sometime later you decide to add a `FavoriteColor` field, you would add a migration:

```sql
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
```

### Querying and Executing SQL

The first thing you'll do is implement `AbstractDatastore`. This just requires a constructor to pass through the connection and transaction. Then you'll add your own methods to execute queries using the provided connection and transaction. `BlendInteractive.Datastore` does not require any particular SQL frameworks, but it works well with something like Dapper.

The methods you provide can be synchronous or asynchronous.

```csharp
    public class TestDatastore : AbstractDatastore
    {
        // Required
        public TestDatastore(SqlConnection connection, SqlTransaction transaction) : base(connection, transaction)
        {
        }

        // Everything else is custom depending on your needs.
        public int GetCountHasFavoriteColor()
        {
            // Always pass both `Connection` and `Transaction` to your query.
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Person WHERE FavoriteColor IS NOT NULL", Connection, Transaction);
            using (var reader = cmd.ExecuteReader())
            {
                return reader.Read() ? reader.GetInt32(0) : -1;
            }
        }

        public async Task<int> GetCountHasFavoriteColorAsync()
        {
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Person WHERE FavoriteColor IS NOT NULL", Connection, Transaction);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                return (await reader.ReadAsync()) ? reader.GetInt32(0) : -1;
            }
        }
    }
```

Next you'll implement `AbstractDatastoreFactory<T>` where `T` will be your `AbstractDatastore` implementation. Each instance of the `AbstractDatastoreFactory` will attempt migrations, so it's a good idea to make sure your instance is a singleton.

```csharp
    public class TestDatastoreFactory : AbstractDatastoreFactory<TestDatastore>
    {
        public TestDatastoreFactory(string connectionString) : base(connectionString)
        {
        }

        // Usually `ProjectName.Namespace` - This is the prefix for the embedded SQL migration file paths.
        public override string SqlResourcesPrefix => "BlendInteractive.Datastore.Tests.Migrations";

        // The name of the stored procedure you use to track the DB version. This should match what's in the ####.sql files.
        protected override string GetVersionProcedureName => "DatabaseVersion";

        // This is the final value your stored procedure should return. For example, if you have 0000.sql and 0001.sql, CurrentVersion should would be 2.
        // In this example, there is only 0000.sql, so it's 1.
        protected override int CurrentVersion => 1;

        // Just need to create a new TestDatastore here, or whatever type your T is.
        protected override TestDatastore GetDatastore(SqlConnection conn, SqlTransaction trans)
            => new TestDatastore(conn, trans);
    }
```

To actually use your datastore, you will use either the `Execute` methods, or the `Query` methods of your DatastoreFactory. These methods accept a lambda that provides an instance of your Datastore with an open connection and optionally an open transaction. With the `Execute` methods, your lambda does not return a value. With the `Query` methods, your lambda does return a value. Both the `Execute` and `Query` methods have versions with transactions, and both synchronous and asynchronous variations. For example, there are four `Execute` variations: `Execute`, `ExecuteAsync`, `ExecuteInTransaction`, and `ExecuteInTransactionAsync`.

An example of using an `async` transaction:

```csharp
    var result = await factory.QueryInTransactionAsync(async (db, context) =>
    {
        var updatedRows = await db.ImportData(data);
        if (updatedRows < 100)
        {
            context.RollbackTransaction = true;
            return ImportDataResult.TooFewUpdates;
        }
        else
        {
            return ImportDataResult.Updated;
        }
    });
```

The transaction is committed automatically when your method returns, unless you set `context.RollbackTransaction = true`. Further, connections are closed and disposed automatically as well.

## Caveats

Currently, this system only supports forward/up migrations. It does not migrate to previous versions.