using Npgsql;

namespace Test.Modules
{
    public class PostgreSQLConn
    {
        public string Host = "localhost";
        public string Port = "5432";
        public string DatabaseName = "Test";
        public string Username = "postgres";
        public string Password = "postgres";

        public PostgreSQLConn(string host, string port, string databaseName, string username, string password)
        {
            Host = host;
            Port = port;
            DatabaseName = databaseName;
            Username = username;
            Password = password;
        }

        public PostgreSQLConn() { }
        public NpgsqlConnection CreateConnection()
        {
            NpgsqlConnection dbconn = new NpgsqlConnection($"Host={Host};Port={Port};Database={DatabaseName};Username={Username};Password={Password}");
            return dbconn;
        }
    }
}
