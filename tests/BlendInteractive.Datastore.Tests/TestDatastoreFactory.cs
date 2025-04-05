
using Microsoft.Data.SqlClient;

namespace BlendInteractive.Datastore.Tests
{
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

    public class TestDatastoreFactory2 : TestDatastoreFactory
    {
        public TestDatastoreFactory2(string connectionString) : base(connectionString)
        {
        }

        // Silly hack for easier migration testing
        protected override int CurrentVersion => 2;
    }

    public class TestDatastoreFactory3 : TestDatastoreFactory
    {
        public TestDatastoreFactory3(string connectionString) : base(connectionString)
        {
        }

        protected override int CurrentVersion => 3;
    }
}
