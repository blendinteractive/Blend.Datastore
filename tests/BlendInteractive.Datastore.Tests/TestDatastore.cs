using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace BlendInteractive.Datastore.Tests
{
    public class TestDatastore : AbstractDatastore
    {
        public TestDatastore(SqlConnection connection, SqlTransaction transaction) : base(connection, transaction)
        {
        }

        public int GetCurrentVersion()
        {
            var cmd = new SqlCommand("EXECUTE DatabaseVersion", Connection, Transaction);
            using (var reader = cmd.ExecuteReader())
            {
                return reader.Read() ? reader.GetInt32(0) : -1;
            }
        }

        public async Task<int> GetCurrentVersionAsync()
        {
            var cmd = new SqlCommand("EXECUTE DatabaseVersion", Connection, Transaction);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                return (await reader.ReadAsync()) ? reader.GetInt32(0) : -1;
            }
        }

        public int GetCountHasFavoriteColor()
        {
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

        public int GetCountNoFavoriteColor()
        {
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Person WHERE FavoriteColor IS NULL", Connection, Transaction);
            using (var reader = cmd.ExecuteReader())
            {
                return reader.Read() ? reader.GetInt32(0) : -1;
            }
        }

        public async Task<int> GetCountNoFavoriteColorAsync()
        {
            var cmd = new SqlCommand("SELECT COUNT(*) FROM Person WHERE FavoriteColor IS NULL", Connection, Transaction);
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                return (await reader.ReadAsync()) ? reader.GetInt32(0) : -1;
            }
        }

        public async Task InsertAsync(PersonRecord person)
        {
            await ExecuteNonQueryAsync($"INSERT INTO Person (Email, FullName, FavoriteColor) VALUES ({person.Email}, {person.FullName}, {person.FavoriteColor});");
        }

        public async Task<PersonRecord> GetByEmailAsync(string email)
        {
            var people = await QueryAsync($"SELECT Id, Email, FullName, FavoriteColor FROM Person WHERE Email = {email}", PersonRecord.FromDataReader);
            return people.Single();
        }

        public void Insert(PersonRecord person)
        {
            ExecuteNonQuery($"INSERT INTO Person (Email, FullName, FavoriteColor) VALUES ({person.Email}, {person.FullName}, {person.FavoriteColor});");
        }

        public PersonRecord GetByEmail(string email)
        {
            var people = Query($"SELECT Id, Email, FullName, FavoriteColor FROM Person WHERE Email = {email}", PersonRecord.FromDataReader);
            return people.Single();
        }

        public void DeleteAllPeople()
        {
            var cmd = new SqlCommand("DELETE FROM Person;", Connection, Transaction);
            cmd.ExecuteNonQuery();
        }

        public async Task DeleteAllPeopleAsync()
        {
            var cmd = new SqlCommand("DELETE FROM Person;", Connection, Transaction);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
