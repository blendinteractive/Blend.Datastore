using System.Data.SqlClient;

namespace Blend.Datastore
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
