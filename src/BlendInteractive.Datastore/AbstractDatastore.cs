using System.Data.SqlClient;

namespace BlendInteractive.Datastore
{
    public abstract class AbstractDatastore
    {
        protected AbstractDatastore(SqlConnection connection, SqlTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        protected SqlConnection Connection { get; }
        protected SqlTransaction Transaction { get; }
    }
}
