using System.Text.Json;

namespace ServerApp
{

    public class Config
    {
        public string PostgresHost { get; set; } = "localhost";
        public int PostgresPort { get; set; } = 5432;
        public string PostgresUser { get; set; } = "postgres";
        public string PostgresPassword { get; set; } = "12345678";
        public int ApiPort { get; set; } = 8080;
        public string Database { get; set; } = "acc";
        public string SchemaPath { get; set; } = "schema.json";

        public static string GetConfigPath(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--conf="))
                {
                    return arg.Substring(7);
                }
            }
            return "server.conf";
        } // ����� ������ GetConfigPath

        // ����� ��� �������� ������������ �� JSON-�����
        public static Config LoadFromJson(string filePath)
        {
            // ��������� ���� �� ���������, ���� ���� �� �������
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.conf");
                Console.WriteLine($"���� ������������ �� �����. ������������ ���� �� ���������: {filePath}. ������ ���� ����� ���������� --conf=filePath.conf");
            }

            // ���� ���� �� ����������, ������ ��� � ���������� �� ���������
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"���� ������������ �� ������. �������� ����� ���� �� ���������: {filePath}");
                var defaultConfig = new Config();
                defaultConfig.SaveToJson(filePath);
                return defaultConfig;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // ������������� ��������
                    ReadCommentHandling = JsonCommentHandling.Skip, // ������� ������������
                    AllowTrailingCommas = true // ��������� ����������� �������
                };

                // �������������� � �������������� �������� �� ���������
                var jsonConfig = JsonSerializer.Deserialize<Config>(File.ReadAllText(filePath), options);
                return jsonConfig ?? new Config();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ��� ������ ������������: {ex.Message}. ������������ ��������� �� ���������.");
                return new Config();
            }
        } 

        // ����� ��� ���������� ������������ � ���� JSON
        public void SaveToJson(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // �������� ��������������
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // ���������� camelCase
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"���� ������������ ������: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ��� �������� ����� ������������: {ex.Message}");
            }
        } 
    }
}