using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Users;
using Materials;
using Suppliers;
using Supply;
using Spend;
using Equipment;
using Reports;
using Auth;
using System.Dynamic;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace ServerApp
{
    // Интерфейс для вызова контроллеров
    public interface IController
    {
        public const string Controller = "";
        dynamic Handle(HttpContext context, string? method);
        static dynamic GetInterface() => throw new NotImplementedException();
    }

    public class ServerApp
    {
        public static Config? config;
        public static Session? session;
        public static User? user;

        public static void Run(string[] args)
        {
            // Чтение конфигурации -- ожидаем, что путь будет либо передан в параметрах, либо взят по умолчанию.
            config = Config.LoadFromJson(Config.GetConfigPath(args));

            if (config == null)
            {
                Console.WriteLine("Ошибка: файл конфигурации не найден или некорректен.");
                return;
            }

            // Инициализация PostgreSQL
            if (!DatabaseHelper.TestDatabaseConnection())
            {
                Console.WriteLine("Ошибка подключения к базе данных. Завершение работы.");
                return;
            }

            Console.WriteLine("Подключение к базе данных успешно.");


            ServerStatus.Status = 0; // Неизвестен
            // Запуск API-сервера
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls($"http://localhost:{config.ApiPort}");
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapMethods("/api/v{version}/{controller}/{method}", new[] { "GET", "POST" }, async context =>
                            {
                                var version = (string?)context.Request.RouteValues["version"];
                                var controller = (string?)context.Request.RouteValues["controller"];
                                var method = (string?)context.Request.RouteValues["method"];
                                context.Response.ContentType = "application/json";
                                object? result = null;
                                IController? controllerObject = null; // Стандартный интерфейс контроллера для обработки запросов
                                // Контроллеры авторизации и пользователя используем практически при каждом запросе, поэтому они создаются всегда.
                                AuthController authController = new(new AuthService());
                                UserController userController = new(new UserService());

                                int? session_id = AuthController.GetSessionId(context);

                                if (session_id != null)
                                {
                                    session = authController.GetSession(session_id ?? 0);
                                    if (session != null && session.isValid())
                                    {
                                        user = userController.Get(session.user_id);
                                    }
                                }

                                bool accessAllowed = controller switch
                                {
                                    // Авторизация
                                    AuthController.Controller or "system" => true,

                                    UserController.Controller => user?.isAdmin() ?? false,
                                    MaterialController.Controller
                                    or SpendController.Controller
                                    or SupplyController.Controller
                                    or SupplierController.Controller
                                    or EquipmentController.Controller =>
                                      (user?.isDirector() ?? false) || (user?.isAccounter() ?? false),
                                    ReportController.Controller => user?.id != null,
                                    _ => false
                                };


                                switch (version)
                                {
                                    case "1":
                                        switch (controller)
                                        {
                                            case AuthController.Controller:
                                                controllerObject = authController;
                                                break;
                                            case UserController.Controller:
                                                controllerObject = userController;
                                                break;
                                            case MaterialController.Controller:
                                                controllerObject = new MaterialController(new MaterialService());
                                                break;
                                            case SupplierController.Controller:
                                                controllerObject = new SupplierController(new SupplierService());
                                                break;
                                            case SupplyController.Controller:
                                                controllerObject = new SupplyController(new SupplyService());
                                                break;
                                            case SpendController.Controller:
                                                controllerObject = new SpendController(new SpendService());
                                                break;
                                            case EquipmentController.Controller:
                                                controllerObject = new EquipmentController(new EquipmentService());
                                                break;
                                            case ReportController.Controller:
                                                controllerObject = new ReportController(new ReportService());
                                                break;
                                            case "system":
                                                switch (method)
                                                {
                                                    case "get-interface":
                                                        result = InterfaceAggregator.GetFullInterface();
                                                        break;
                                                    case "check-health":
                                                        var status = ServerStatus.Status;
                                                        if (status == -1)
                                                        {
                                                            context.Response.StatusCode = 503;
                                                            result = new { message = "Ведутся работы на сервере" };
                                                        }
                                                        else
                                                        {
                                                            result = new { message = "ОК" };
                                                        }
                                                        break;
                                                }
                                                break;
                                            // TODO: добавить остальные контроллеры
                                            default:
                                                context.Response.StatusCode = 404;
                                                result = new { message = "Контроллер не найден" };
                                                break;
                                        }
                                        break;
                                    default:
                                        context.Response.StatusCode = 501;
                                        result = new { message = "Версия API не поддерживается. Используйте v1." };
                                        break;
                                }
                                if (controllerObject != null)
                                {
                                    if (accessAllowed)
                                    {
                                        result = controllerObject.Handle(context, method);
                                    }
                                    else
                                    {
                                        context.Response.StatusCode = 401;
                                        result = new
                                        {
                                            success = false,
                                            message = $"Ваша роль \"{user?.role ?? "Не авторизован"}\" не позволяет использовать контроллер \"{controller}\"."
                                        };
                                    }

                                }

                                var options = new JsonSerializerOptions
                                {
                                    DefaultIgnoreCondition = JsonIgnoreCondition.Never, // Включаем свойства с null значениями
                                    WriteIndented = false // Если нужен компактный JSON
                                };

                                string resultText = JsonSerializer.Serialize(result, options);
                                await context.Response.WriteAsync(resultText);
                            });

                            endpoints.MapMethods("/{**filePath}", new[] { "GET" }, async context =>
                            {
                                string filePath = context.Request.RouteValues["filePath"]?.ToString() ?? "";
                                string baseDirectory = Path.Combine(AppContext.BaseDirectory, "AccountingClient");
                                string fullPath = Path.Combine(baseDirectory, filePath);

                                if (!File.Exists(fullPath))
                                {
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("404 Not Found");
                                    return;
                                }

                                // Определение Content-Type
                                string extension = Path.GetExtension(fullPath).ToLowerInvariant();
                                string contentType = extension switch
                                {
                                    ".html" or ".htm" => "text/html",
                                    ".js" => "application/javascript",
                                    ".css" => "text/css",
                                    ".jpg" or ".jpeg" => "image/jpeg",
                                    ".png" => "image/png",
                                    ".gif" => "image/gif",
                                    _ => "application/octet-stream"
                                };

                                context.Response.ContentType = contentType;

                                try
                                {
                                    await context.Response.SendFileAsync(fullPath); // Отправляем файл
                                }
                                catch (Exception ex)
                                {
                                    context.Response.StatusCode = 500;
                                    await context.Response.WriteAsync($"500 Internal Server Error: {ex.Message}");
                                }
                            });
                        });
                    });
                })
                .Build();

            Console.WriteLine($"Сервер запущен на порту {config.ApiPort}. Нажмите Ctrl+C для завершения.");
            host.RunAsync();

            if (DatabaseHelper.RunMigration()) // Проверяем существование базы
            {
                ServerStatus.Status = 1; // Всё в порядке, работаем дальше
            }
            else
            {
                Console.WriteLine("Завершение работы сервера...");
                return;
            }


            Console.WriteLine("Введите 'exit' или 'q', чтобы завершить сервер, app для запуска браузера с клиентом.");
            bool run = true;
            while (run)
            {
                var command = Console.ReadLine();
                switch (command?.ToLower())
                {
                    case "exit":
                    case "q":
                    case "й":
                    case "\\q":
                    case "stop":
                    case "quit":
                    case "выход":
                        Console.WriteLine("Завершение работы сервера и выход...");
                        run = false;
                        break;
                    case "app":
                        string url = $"http://localhost:{config.ApiPort}/index.html";
                        OpenBrowser(url);
                        break;
                    default:
                        Console.WriteLine("Введите 'exit' или 'q', чтобы завершить сервер, app для запуска браузера с клиентом.");
                        break;
                }
            }
        }

        public static int? getInt(HttpContext context, string name, int? defaultValue = null)
        {
            string s = context.Request.Query[name];
            int result;
            bool isNull = false;

            if (string.IsNullOrEmpty(s) || !int.TryParse(s, out result))
            {
                result = defaultValue ?? 0; // Устанавливаем значение по умолчанию
                isNull = true;
            }

            return isNull ? null : result;
        }
        public static DateTime? getDateTime(HttpContext context, string name, DateTime? defaultValue = null)
        {
            string s = context.Request.Query[name];
            DateTime result;
            bool isNull = false;

            if (string.IsNullOrEmpty(s) || !DateTime.TryParse(s, out result))
            {
                result = defaultValue ?? DateTime.Today; // Устанавливаем значение по умолчанию
                isNull = true;
            }

            return isNull ? null : result;
        }



        static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Использует системный обработчик для открытия ссылки
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии браузера: {ex.Message}");
            }
        }
    }

    public class ServerResponse
    {
        public int? Code { get; set; } = 200;
        public string? Message { get; set; }

        public dynamic? Data { get; set; }
    }

    public static class ServerStatus
    {
        private static volatile int _status;

        public static int Status
        {
            get => _status;
            set => _status = value;
        }
    }

    public static class InterfaceAggregator
    {
        private static readonly List<Type> Controllers = new()
    {
        typeof(UserController),
        typeof(MaterialController),
        typeof(SupplierController),
        typeof(EquipmentController),
        typeof(SupplyController),
        typeof(SpendController),
        typeof(ReportController)
    };

        public static dynamic GetFullInterface()
        {
            dynamic result = new ExpandoObject();
            var controllers = new List<Type>
            {
                typeof(UserController),
                typeof(MaterialController),
                typeof(SupplierController),
                typeof(EquipmentController),
                typeof(SupplyController),
                typeof(SpendController),
                typeof(ReportController)
            };

            foreach (var controller in controllers)
            {
                var method = controller.GetMethod("GetInterface", BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    var controllerInterface = method.Invoke(null, null);
                    foreach (var kv in (IDictionary<string, object>)controllerInterface)
                    {
                        ((IDictionary<string, object>)result)[kv.Key] = kv.Value;
                    }
                }
            }

            return result;
        }

    }

}