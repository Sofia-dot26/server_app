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
        } // Конец метода GetConfigPath

        // Метод для загрузки конфигурации из JSON-файла
        public static Config LoadFromJson(string filePath)
        {
            // Установка пути по умолчанию, если путь не передан
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server.conf");
                Console.WriteLine($"Файл конфигурации не задан. Используется файл по умолчанию: {filePath}. Задать файл можно аргументом --conf=filePath.conf");
            }

            // Если файл не существует, создаём его с значениями по умолчанию
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл конфигурации не найден. Создаётся новый файл по умолчанию: {filePath}");
                var defaultConfig = new Config();
                defaultConfig.SaveToJson(filePath);
                return defaultConfig;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // Игнорирование регистра
                    ReadCommentHandling = JsonCommentHandling.Skip, // Пропуск комментариев
                    AllowTrailingCommas = true // Поддержка завершающих запятых
                };

                // Десериализация с использованием значений по умолчанию
                var jsonConfig = JsonSerializer.Deserialize<Config>(File.ReadAllText(filePath), options);
                return jsonConfig ?? new Config();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении конфигурации: {ex.Message}. Используются параметры по умолчанию.");
                return new Config();
            }
        } 

        // Метод для сохранения конфигурации в файл JSON
        public void SaveToJson(string filePath)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // Красивое форматирование
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Используем camelCase
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(filePath, json);
                Console.WriteLine($"Файл конфигурации создан: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании файла конфигурации: {ex.Message}");
            }
        } 
    }
}