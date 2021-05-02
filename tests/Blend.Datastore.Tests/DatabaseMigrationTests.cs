using System;
using System.Threading.Tasks;
using Xunit;

namespace Blend.Datastore.Tests
{
    public class DatabaseMigrationTests
    {
        // Terrible way to do this, but make this a connection string to an empty SQL database.
        private static readonly string ConnectionString = "Server=.;Database=DatastoreTests;Integrated Security=true;";

        [Fact]
        public void MigrationsHappen()
        {
            try
            {
                // Apply version 1
                var factory = new TestDatastoreFactory(ConnectionString);
                int currentVersion = factory.Query(db => db.GetCurrentVersion());
                Assert.Equal(1, currentVersion);

                // Re-apply version 1
                var factory2 = new TestDatastoreFactory(ConnectionString);
                int currentVersion2 = factory2.Query(db => db.GetCurrentVersion());
                Assert.Equal(1, currentVersion2);

                // Apply version 2
                var factory3 = new TestDatastoreFactory2(ConnectionString);
                int currentVersion3 = factory3.Query(db => db.GetCurrentVersion());
                Assert.Equal(2, currentVersion3);

                // Test that results make sense. 3 fictional characters do not have a favorite color. But Batman does, of course.
                var (hasFavorite, noFavorite) = factory3.Query(db => (db.GetCountHasFavoriteColor(), db.GetCountNoFavoriteColor()));
                Assert.Equal(1, hasFavorite);
                Assert.Equal(3, noFavorite);
            }
            finally
            {
                // Clean up, version 3 (0002.sql) cleans up DB
                var cleanupFactory = new TestDatastoreFactory3(ConnectionString);
                cleanupFactory.EnsureMigration();
            }
        }

        [Fact]
        public void CanAbortTransactions()
        {
            try
            {
                var factory = new TestDatastoreFactory2(ConnectionString);
                int currentVersion = factory.Query(db => db.GetCurrentVersion());
                Assert.Equal(2, currentVersion);

                // Test that results make sense. 3 fictional characters do not have a favorite color. But Batman does, of course.
                var (hasFavorite, noFavorite) = factory.Query(db => (db.GetCountHasFavoriteColor(), db.GetCountNoFavoriteColor()));
                Assert.Equal(1, hasFavorite);
                Assert.Equal(3, noFavorite);

                // Delete them all, but rollback the transaction
                factory.ExecuteInTransaction((db, context) =>
                {
                    db.DeleteAllPeople();
                    context.RollbackTransaction = true;
                });

                // These should not have changed
                (hasFavorite, noFavorite) = factory.Query(db => (db.GetCountHasFavoriteColor(), db.GetCountNoFavoriteColor()));
                Assert.Equal(1, hasFavorite);
                Assert.Equal(3, noFavorite);

                // This time commit the transaction
                factory.ExecuteInTransaction((db, context) =>
                {
                    db.DeleteAllPeople();
                });

                // These should be no survivors
                (hasFavorite, noFavorite) = factory.Query(db => (db.GetCountHasFavoriteColor(), db.GetCountNoFavoriteColor()));
                Assert.Equal(0, hasFavorite);
                Assert.Equal(0, noFavorite);
            }
            finally
            {
                // Clean up, version 3 (0002.sql) cleans up DB
                var cleanupFactory = new TestDatastoreFactory3(ConnectionString);
                cleanupFactory.EnsureMigration();
            }
        }

        [Fact]
        public async Task CanAbortTransactionsAsync()
        {
            try
            {
                var factory = new TestDatastoreFactory2(ConnectionString);
                int currentVersion = await factory.QueryAsync(db => db.GetCurrentVersionAsync());
                Assert.Equal(2, currentVersion);

                // Test that results make sense. 3 fictional characters do not have a favorite color. But Batman does, of course.
                var (hasFavorite, noFavorite) = await factory.QueryAsync(async db => (await db.GetCountHasFavoriteColorAsync(), (await db.GetCountNoFavoriteColorAsync())));
                Assert.Equal(1, hasFavorite);
                Assert.Equal(3, noFavorite);

                // Delete them all, but rollback the transaction
                await factory.ExecuteInTransactionAsync(async (db, context) =>
                {
                    await db.DeleteAllPeopleAsync();
                    context.RollbackTransaction = true;
                });

                // These should not have changed
                (hasFavorite, noFavorite) = await factory.QueryAsync(async db => ((await db.GetCountHasFavoriteColorAsync()), (await db.GetCountNoFavoriteColorAsync())));
                Assert.Equal(1, hasFavorite);
                Assert.Equal(3, noFavorite);

                // This time commit the transaction
                await factory.ExecuteInTransactionAsync(async (db, context) =>
                {
                    await db.DeleteAllPeopleAsync();
                });

                // These should be no survivors
                (hasFavorite, noFavorite) = await factory.QueryAsync(async db => ((await db.GetCountHasFavoriteColorAsync()), (await db.GetCountNoFavoriteColorAsync())));
                Assert.Equal(0, hasFavorite);
                Assert.Equal(0, noFavorite);
            }
            finally
            {
                // Clean up, version 3 (0002.sql) cleans up DB
                var cleanupFactory = new TestDatastoreFactory3(ConnectionString);
                cleanupFactory.EnsureMigration();
            }
        }
    }
}
