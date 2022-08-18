using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlendInteractive.Datastore
{
    public abstract class AbstractDatastoreFactory<TDatastore> where TDatastore : AbstractDatastore
    {
        private readonly string connectionString;

        protected AbstractDatastoreFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        const int NOT_INSTALLED = 0;
        protected abstract TDatastore GetDatastore(SqlConnection conn, SqlTransaction trans);
        public abstract string SqlResourcesPrefix { get; }
        protected abstract string GetVersionProcedureName { get; }
        protected abstract int CurrentVersion { get; }
        bool MigrationDone = false;
        
        /// <summary>
        /// Returns TRUE if 0000.sql contains the full DB from scratch. In other words, starting from a blank
        /// database, only 0000.sql needs to be run to install the full database. Ohterwise, it will run every
        /// ####.sql file until CurrentVersion.
        /// </summary>
        protected virtual bool FirstMigrationIsComplete => false;

        public void Execute(Action<TDatastore> action)
        {
            EnsureMigration();

            using (var conn = GetConn())
            {
                var datastore = GetDatastore(conn, null);
                action(datastore);
            }
        }

        public async Task ExecuteAsync(Func<TDatastore, Task> action)
        {
            EnsureMigration();

            using (var conn = await GetConnAsync())
            {
                var datastore = GetDatastore(conn, null);
                await action(datastore);
            }
        }

        public void ExecuteInTransaction(Action<TDatastore, ITransactionContext> action)
        {
            EnsureMigration();

            using (var conn = GetConn())
            using (var trans = conn.BeginTransaction())
            {
                var datastore = GetDatastore(conn, trans);
                var context = new DefaultTransactionContext();
                action(datastore, context);
                if (context.RollbackTransaction)
                    trans.Rollback();
                else
                    trans.Commit();
            }
        }

        public async Task ExecuteInTransactionAsync(Func<TDatastore, ITransactionContext, Task> action)
        {
            EnsureMigration();

            using (var conn = await GetConnAsync())
            using (var trans = conn.BeginTransaction())
            {
                var datastore = GetDatastore(conn, trans);
                var context = new DefaultTransactionContext();
                await action(datastore, context);
                if (context.RollbackTransaction)
                    trans.Rollback();
                else
                    trans.Commit();

            }
        }

        public T Query<T>(Func<TDatastore, T> action)
        {
            EnsureMigration();

            using (var conn = GetConn())
            {
                var datastore = GetDatastore(conn, null);
                T result = action(datastore);
                return result;
            }
        }

        public async Task<T> QueryAsync<T>(Func<TDatastore, Task<T>> action)
        {
            EnsureMigration();

            using (var conn = await GetConnAsync())
            {
                var datastore = GetDatastore(conn, null);
                T result = await action(datastore);
                return result;
            }
        }

        public T QueryInTransaction<T>(Func<TDatastore, ITransactionContext, T> action)
        {
            EnsureMigration();

            using (var conn = GetConn())
            using (var trans = conn.BeginTransaction())
            {
                var datastore = GetDatastore(conn, trans);
                var context = new DefaultTransactionContext();
                var result = action(datastore, context);
                if (context.RollbackTransaction)
                    trans.Rollback();
                else
                    trans.Commit();

                return result;
            }
        }

        public async Task<T> QueryInTransactionAsync<T>(Func<TDatastore, ITransactionContext, Task<T>> action)
        {
            EnsureMigration();

            using (var conn = await GetConnAsync())
            using (var trans = conn.BeginTransaction())
            {
                var datastore = GetDatastore(conn, trans);
                var context = new DefaultTransactionContext();
                var result = await action(datastore, context);
                if (context.RollbackTransaction)
                    trans.Rollback();
                else
                    trans.Commit();
                return result;
            }
        }

        SqlConnection GetConn()
        {
            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }

        async Task<SqlConnection> GetConnAsync()
        {
            var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            return conn;
        }


        int GetDatabaseVersion()
        {
            using (var conn = GetConn())
            {
                using (var cmd = new SqlCommand($"SELECT CASE WHEN OBJECT_ID('{GetVersionProcedureName}') IS NULL THEN CAST(0 as bit) ELSE CAST(1 as bit) END", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    bool procExists = reader.Read() && reader.GetBoolean(0);
                    if (!procExists)
                        return NOT_INSTALLED;
                }

                using (var cmd = new SqlCommand($"EXECUTE {GetVersionProcedureName}", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? reader.GetInt32(0) : NOT_INSTALLED;
                }
            }
        }

        public void EnsureMigration()
        {
            if (MigrationDone)
                return;

            int currentVersion = GetDatabaseVersion();

            if (currentVersion < CurrentVersion)
            {

                using (var conn = GetConn())
                using (var trans = conn.BeginTransaction())
                {
                    while (currentVersion < CurrentVersion)
                    {
                        var chunks = GetSqlFromResource(currentVersion);
                        foreach (var chunk in chunks)
                        {
                            using (SqlCommand cmd = new SqlCommand(chunk, conn, trans))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }

                        if (FirstMigrationIsComplete && currentVersion == NOT_INSTALLED)
                            break;

                        currentVersion += 1;
                    }
                    trans.Commit();
                }
            }

            MigrationDone = true;
        }

        // Matches "GO" followed by semicolons, whitespace, or inline comment.
        // NOTE: `GO /* blah blah */` will break this. Not going to write a full SQL parser here.
        private static readonly Regex GoRegex = new Regex(@"^go[\s;]*(--.+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected virtual string SqlFileNamePattern => "000#";

        IEnumerable<string> GetSqlFromResource(int versionId)
        {
            using (var stream = GetType().Assembly.GetManifestResourceStream($"{SqlResourcesPrefix}.{versionId.ToString(SqlFileNamePattern)}.sql"))
            {
                var reader = new StreamReader(stream);

                string line;
                do
                {
                    var buffer = new StringWriter();

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        if (GoRegex.IsMatch(line.Trim()))
                            break;

                        buffer.WriteLine(line);
                    }

                    var chunk = buffer.ToString();
                    if (!string.IsNullOrEmpty(chunk))
                        yield return chunk;
                } while (line != null);
            }
        }

        class DefaultTransactionContext : ITransactionContext
        {
            public bool RollbackTransaction { get; set; }
        }
    }
}
