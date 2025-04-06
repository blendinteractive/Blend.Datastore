using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BlendInteractive.Datastore.Tests
{
    public class DatabaseMigrationTests
    {
        // Terrible way to do this, but make this a connection string to an empty SQL database.
        private static readonly string ConnectionString = "Server=.;Database=DatastoreTests;Integrated Security=true;Encrypt=No";

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

        [Fact]
        public async Task CanUseAsyncConvenienceMethods()
        {
            try
            {
                var factory = new TestDatastoreFactory2(ConnectionString);
                int currentVersion = factory.Query(db => db.GetCurrentVersion());
                Assert.Equal(2, currentVersion);

                var bobbyTables = new PersonRecord
                {
                    Email = "test@example.com",
                    FullName = "'; DROP TABLE Person; --",
                    FavoriteColor = "red"
                };

                await factory.ExecuteAsync(db => db.InsertAsync(bobbyTables));

                var bobbyBack = await factory.QueryAsync(db => db.GetByEmailAsync(bobbyTables.Email));
                Assert.Equal(bobbyTables.Email, bobbyBack.Email);
                Assert.Equal(bobbyTables.FullName, bobbyBack.FullName);
                Assert.Equal(bobbyTables.FavoriteColor, bobbyBack.FavoriteColor);


                var sallyTables = new PersonRecord
                {
                    Email = "test2@example.com",
                    FullName = "'; DROP TABLE Person; DROP PROCEDURE DatabaseVersion; --",
                    FavoriteColor = "asdf"
                };

                await factory.ExecuteAsync(db => db.InsertAsync(sallyTables));

                var sallyBack = await factory.QueryAsync(db => db.GetByEmailAsync(sallyTables.Email));
                Assert.Equal(sallyTables.Email, sallyBack.Email);
                Assert.Equal(sallyTables.FullName, sallyBack.FullName);
                Assert.Equal(sallyTables.FavoriteColor, sallyBack.FavoriteColor);
            }
            finally
            {
                // Clean up, version 3 (0002.sql) cleans up DB
                var cleanupFactory = new TestDatastoreFactory3(ConnectionString);
                cleanupFactory.EnsureMigration();
            }
        }

        [Fact]
        public void CanUseConvenienceMethods()
        {
            try
            {
                var factory = new TestDatastoreFactory2(ConnectionString);
                int currentVersion = factory.Query(db => db.GetCurrentVersion());
                Assert.Equal(2, currentVersion);

                var bobbyTables = new PersonRecord
                {
                    Email = "test@example.com",
                    FullName = "'; DROP TABLE Person; --",
                    FavoriteColor = "red"
                };

                factory.Execute(db => db.Insert(bobbyTables));

                var bobbyBack = factory.Query(db => db.GetByEmail(bobbyTables.Email));
                Assert.Equal(bobbyTables.Email, bobbyBack.Email);
                Assert.Equal(bobbyTables.FullName, bobbyBack.FullName);
                Assert.Equal(bobbyTables.FavoriteColor, bobbyBack.FavoriteColor);


                var sallyTables = new PersonRecord
                {
                    Email = "test2@example.com",
                    FullName = "'; DROP TABLE Person; DROP PROCEDURE DatabaseVersion; --",
                    FavoriteColor = "asdf"
                };

                factory.Execute(db => db.Insert(sallyTables));

                var sallyBack = factory.Query(db => db.GetByEmail(sallyTables.Email));
                Assert.Equal(sallyTables.Email, sallyBack.Email);
                Assert.Equal(sallyTables.FullName, sallyBack.FullName);
                Assert.Equal(sallyTables.FavoriteColor, sallyBack.FavoriteColor);
            }
            finally
            {
                // Clean up, version 3 (0002.sql) cleans up DB
                var cleanupFactory = new TestDatastoreFactory3(ConnectionString);
                cleanupFactory.EnsureMigration();
            }
        }

        [Fact]
        public void CanUseNulls()
        {
            try
            {
                var factory = new TestDatastoreFactory2(ConnectionString);
                int currentVersion = factory.Query(db => db.GetCurrentVersion());
                Assert.Equal(2, currentVersion);

                var nullColors = new PersonRecord
                {
                    Email = "nullColors@example.com",
                    FullName = "Null Colours",
                    FavoriteColor = null
                };

                factory.Execute(db => db.Insert(nullColors));

                var colorsBack = factory.Query(db => db.GetByEmail(nullColors.Email));
                Assert.Equal(nullColors.Email, colorsBack.Email);
                Assert.Equal(nullColors.FullName, colorsBack.FullName);
                Assert.Null(colorsBack.FavoriteColor);
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
