using Microsoft.Data.SqlClient;

namespace FigAssessmentCodeReviews.CodeReviews
{
    /*
     * These are the things that I'm going to assume:
     * .NET version 8.0
     * We have DI configuration in our root file
     * We are not using global exception handling
    */

    // Would you mind making an Interface that this service could inherit? This promotes decoupling for the components that want an
    // instance of this service. It could also make testing the components that utilize this service easier, as we could make a 'TestService'
    // that inherits said interface, then tell the DI config to use that 'TestService'.
    public class UserService
    {
        // I believe there should be an appsettings value that we can pull in, instead of hardcoding this! If there isn't, would
        // you mind making one?
        private readonly string _connectionString = "Server=prod-db01;Database=UserDB;User Id=sa;Password=MySecretPassword123!;";

        // Small change, but since there is a possibilty of returning a null value, could we make the return type nullable?
        // Can we also pass in CancellationTokens from our controllers and feed them downstream to our SqlClient calls?
        public async Task<User> GetUserByIdAsync(int userId)
        {
            // Great thinking, but I would let exceptions bubble up, and have the controller handle them and logging!
            try
            {
                // Could we add an await at the beginning of this line, before 'using var'? The connection would block on cleanup,
                // so adding await will free the thread to do other stuff in the mean time!
                using var connection = new SqlConnection(_connectionString);

                // Could we add an await at the begining of this line and change .Open() to .OpenAsync()? Currently, this would
                // block while talking to SQL server, which could take some time!
                connection.Open();

                // I really love string interpolation, it looks so clean, but in this situation it could unfortunately
                // leave us vulnerable to SQL Injection. Would you mind using a Parameterized Query?
                // Also, SELECT * is perfect if we know we need every column, but it could affect performance if we don't
                // need all of them! Would you mind declaring the columns that we need in the select list?
                var query = $"SELECT * FROM Users WHERE Id = {userId}";
                using var command = new SqlCommand(query, connection);

                // Sheesh! .ExecuteReader() seems a little harsh... I don't know what the reader did, but could we add an await
                // after the equals sign and use .ExecuteReaderAsync() so I don't have to be around for that?
                // Could we also add an await at the begining of this line, before 'using var'? Currently, the reader would
                // block on cleanup, so adding await will let the thread do other stuff in the mean time!
                // It's also good practice to include the CommandBehavior when calling ExecuteReader, as it can improve performance!
                using var reader = command.ExecuteReader();

                // Could we add an await before 'reader' and change .Read() to .ReadAsync()?
                // Currently, this would wait on the network for the next row, so we want to release the thread if it does have to wait
                if (reader.Read())
                {
                    return new User
                    {
                        // Indexing and casting totally works, but it could be a little bit dangerous. Do you think we could use ordinals
                        // and typed getters to avoid DBNull issues and unsafe casts?
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString(),
                        Email = reader["Email"].ToString(),
                        Password = reader["Password"].ToString(), // Returning password hash
                        CreatedDate = (DateTime)reader["CreatedDate"],
                        IsActive = (bool)reader["IsActive"],
                        Role = reader["Role"].ToString()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log exception somewhere

                throw ex;
            }
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            // Looks like this needs the same update mentioned in lines 28-29!
            using var connection = new SqlConnection(_connectionString);

            // This line needs the same update mentioned in lines 32-33!
            connection.Open();

            // This one needs the same update as mentioned in lines 36-37!
            var query = $"SELECT COUNT(*) FROM Users WHERE Username = '{username}' AND Password = '{password}'";
            using var command = new SqlCommand(query, connection);

            // Oh no, they're getting Scalar too! Could we update this line using the mentioned edit in lines 43-47,
            // except we'll use .ExecuteScalarAsync()
            var result = (int)command.ExecuteScalar();
            return result > 0;
        }

        // This line would need the update mentioned in line 21-22
        public async Task<User> CreateUserAsync(string username, string email, string password)
        {
            // Looks like this needs the same update mentioned in lines 28-29!
            using var connection = new SqlConnection(_connectionString);

            // This line needs the same update mentioned in lines 32-33!
            connection.Open();

            // This one needs the same update as mentioned in lines 36-37!
            // Also, both queries in this block look fantastic, but this could potentially be a high-traffic endpoint. Would you mind appending
            // the second query to the end of the first, so we only have to make one call over the network? 
            var insertQuery = $"INSERT INTO Users (Username, Email, Password, CreatedDate, IsActive) VALUES ('{username}', '{email}', '{password}', GETDATE(), 1)";
            using var command = new SqlCommand(insertQuery, connection);

            // Because of the change mentioned in lines 106-107 and since we don't want to block while the query executes,
            // would you mind changing this call to be 'using var reader = await command.ExecuteReaderAsync();'?
            command.ExecuteNonQuery();

            // Because of the changes mentioned in lines 106-107 and 111-112, we should be able to remove lines 117-119
            // Get the newly created user
            var selectQuery = $"SELECT TOP 1 * FROM Users WHERE Username = '{username}' ORDER BY CreatedDate DESC";
            using var selectCommand = new SqlCommand(selectQuery, connection);
            using var reader = selectCommand.ExecuteReader();

            // This looks like it needs the same update mentioned in line 50-51!
            if (reader.Read())
            {
                return new User
                {
                    // These lines would need the same update mentioned in lines 56-57!
                    Id = (int)reader["Id"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    Password = reader["Password"].ToString(),
                    CreatedDate = (DateTime)reader["CreatedDate"],
                    IsActive = (bool)reader["IsActive"]
                };
            }

            return null;
        }
    }

    // I might be missing something, but if we're only using this class for returning data from the database
    // do you mind changing all instances of set; to init;? Since our database is our source of truth, we want these
    // properties to be immutable! We should also add the required modifier to the properties that are required
    // and if a string property is not required, we should default it with an empty string! 
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; }
    }
}
