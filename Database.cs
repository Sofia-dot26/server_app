using Npgsql;
using System.Text.Json;

namespace ServerApp
{

    public class DatabaseSchema
    {
        public List<TableSchema>? Tables { get; set; }
    } 

    public class TableSchema
    {
        public string? Name { get; set; }
        public List<ColumnSchema>? Columns { get; set; }
    } 

    public class ColumnSchema
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public bool PrimaryKey { get; set; }
    } 

    public static class DatabaseHelper
    {
        public static bool ExecuteNonQuery(string sql, Dictionary<string, object>? parameters)
        {
            try
            {
                using var connection = new NpgsqlConnection(GetConnectionString());
                connection.Open();
                using var command = new NpgsqlCommand(sql, connection);
                AddParameters(command, parameters);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ���������� SQL: {ex.Message}");
                return false;
            }
        } 

        public static List<Dictionary<string, object?>> ExecuteQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            var results = new List<Dictionary<string, object?>>();

            try
            {
                using var connection = new NpgsqlConnection(GetConnectionString());
                connection.Open();
                using var command = new NpgsqlCommand(sql, connection);
                if (parameters != null)
                {
                    AddParameters(command, parameters);
                }

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ���������� �������: {ex.Message}");
            }

            return results;
        } 

        private static string GetConnectionString(string? database = null)
        {
            var config = ServerApp.config;
            return config == null ? "" : $"Host={config.PostgresHost};Port={config.PostgresPort};Username={config.PostgresUser};Password={config.PostgresPassword};Database={database ?? config.Database}";
        } 

        private static void AddParameters(NpgsqlCommand command, Dictionary<string, object>? parameters)
        {
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
        } 

        public static bool TestDatabaseConnection()
        {
            var config = ServerApp.config;
            var result = false;
            if (config != null)
            {
                Console.WriteLine($"������������ ����������� � PostgreSQL: {config.PostgresHost}:{config.PostgresPort}");

                try
                {
                    using var connection = new NpgsqlConnection(GetConnectionString("postgres"));
                    connection.Open();
                    Console.WriteLine("����������� �������.");
                    result = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"������ �����������: {ex.Message}");
                }
            }
            return result;
        } 


        public static bool RunMigration()
        {
            Config? config = ServerApp.config;
            string migrationFilePath = "migration.sql";

            if (config == null) {
                Console.WriteLine("������ �� ������. �������� �� ���������.");
                return false;
            }

            if (!File.Exists(migrationFilePath))
            {
                Console.WriteLine("���� �������� �� ������. �������� �� ���������.");
                return false;
            }

            string migrationSQL = File.ReadAllText(migrationFilePath);


            try
            {
                // ��� 1: ����������� � ���� postgres
                string connectionStringPostgres = GetConnectionString("postgres");
                bool needMigration = false;
                using (var connection = new Npgsql.NpgsqlConnection(connectionStringPostgres))
                {
                    connection.Open();

                    // ��������� ������������� ���� ������
                    using (var command = new Npgsql.NpgsqlCommand($"SELECT 1 FROM pg_database WHERE datname = '{config.Database}'", connection))
                    {
                        if (command.ExecuteScalar() == null)
                        {
                            // ������ ���� ������
                            command.CommandText = $"CREATE DATABASE {config.Database}";
                            command.ExecuteNonQuery();
                            Console.WriteLine($"���� ������ {config.Database} �������.");
                            // ���� ���� �� ���� � ��� ���� ������� - �������� �����
                            needMigration = true;
                        }
                        else
                        {
                            Console.WriteLine($"���� ������ {config.Database} ��� ����������.");
                        }
                    }
                }
                // �������� �����
                if (needMigration) 
                {
                    // ��� 2: ����������� � ��������� ���� ������
                    string connectionStringAccounting = GetConnectionString(config.Database); ;

                    using (var connection = new Npgsql.NpgsqlConnection(connectionStringAccounting))
                    {
                        connection.Open();

                        // ��������� ������� ��������
                        using (var command = new Npgsql.NpgsqlCommand(migrationSQL, connection))
                        {
                            command.ExecuteNonQuery();
                            Console.WriteLine("�������� ������� ���������.");
                        }
                    }
                }
                // �� � ���� ����� �� � ����� ������ �������, ��� �� �� � ����� �������� ������
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ���������� ��������: {ex.Message}");
                return false;
            }
        } 


    } 
}