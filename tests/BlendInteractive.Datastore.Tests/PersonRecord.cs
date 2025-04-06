using System.Data;

namespace BlendInteractive.Datastore.Tests
{
    public class PersonRecord
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string FavoriteColor { get; set; }

        public static PersonRecord FromDataReader(IDataReader reader)
        {
            // WARNING: This is an OVERLY simplistic implementation.
            // * Assumes consistent column order and count
            // * Does not check for nulls
            // * Really, this is terrible.
            return new PersonRecord
            {
                Id = reader.GetInt32(0),
                Email = reader.GetString(1),
                FullName = reader.GetString(2),
                FavoriteColor = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }
    }
}
