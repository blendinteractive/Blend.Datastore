using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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

        protected SqlCommand PrepareCommand(FormattableString sql)
        {
            var command = Connection.CreateCommand();
            command.Transaction = Transaction;

            var parameters = Enumerable.Range(0, sql.ArgumentCount)
                .Select(x => "@arg" + x.ToString())
                .ToArray();

            command.CommandType = CommandType.Text;
            command.CommandText = string.Format(sql.Format, parameters);

            for (var x = 0; x < parameters.Length; x++)
            {
                var argValue = sql.GetArgument(x);
                var parameter = command.CreateParameter();
                parameter.ParameterName = parameters[x];
                parameter.Value = argValue ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            return command;
        }

        protected IEnumerable<T> Query<T>(FormattableString sql, Func<IDataReader, T> transform, Action<SqlCommand> tweak = null)
        {
            var command = PrepareCommand(sql);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                yield return transform(reader);
            }
        }

        protected async Task<IEnumerable<T>> QueryAsync<T>(FormattableString sql, Func<IDataReader, T> transform, Action<SqlCommand> tweak = null)
        {
            var command = PrepareCommand(sql);
            SqlDataReader reader = await command.ExecuteReaderAsync();
            var output = new List<T>();
            while (await reader.ReadAsync())
            {
                output.Add(transform(reader));
            }
            return output;
        }

        protected object ExecuteScalar(FormattableString sql, Action<SqlCommand> tweak = null)
        {
            var command = PrepareCommand(sql);
            tweak?.Invoke(command);
            return command.ExecuteScalar();
        }

        protected async Task<object> ExecuteScalarAsync(FormattableString sql, Action<SqlCommand> tweak = null)
        {
            var command = PrepareCommand(sql);
            tweak?.Invoke(command);
            return await command.ExecuteScalarAsync();
        }

        protected void ExecuteNonQuery(FormattableString sql, Action<SqlCommand> tweak = null)
        {
            var command = PrepareCommand(sql);
            tweak?.Invoke(command);
            command.ExecuteNonQuery();
        }

        protected async Task ExecuteNonQueryAsync(FormattableString sql, Action<SqlCommand> tweak = null)
        {
            var command = PrepareCommand(sql);
            tweak?.Invoke(command);
            await command.ExecuteNonQueryAsync();
        }
    }
}
