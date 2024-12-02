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

namespace ServerApp
{
    // Интерфейс для вызова контроллеров
    public interface IController
    {
        dynamic Handle(HttpContext context, string? method);
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
                //.ConfigureLogging(logging => logging.ClearProviders()) // Отключу лог перед сдачей, чтоб не мельтешил
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
                                    "auth" or "health" => true,
                                    "users" => user?.isAdmin() ?? false,
                                    "materials" or "spend" or "supplies" => user?.isAccounter() ?? false,
                                    "suppliers" or "equipment" => user?.isDirector() ?? false,
                                    _ => false
                                };
                                

                                switch (version)
                                {
                                    case "1":
                                        switch (controller)
                                        {
                                            case "auth":
                                                controllerObject = authController;
                                                break;
                                            case "users":
                                                controllerObject = userController;
                                                break;
                                            case "materials":
                                                controllerObject = new MaterialController(new MaterialService());
                                                break;
                                            case "suppliers":
                                                controllerObject = new SupplierController(new SupplierService());
                                                break;
                                            case "supplies":
                                                controllerObject = new SupplyController(new SupplyService());
                                                break;
                                            case "spend":
                                                controllerObject = new SpendController(new SpendService());
                                                break;
                                            case "equipment":
                                                controllerObject = new EquipmentController(new EquipmentService());
                                                break;
                                            case "reports":
                                                controllerObject = new ReportController(new ReportService());
                                                break;
                                            case "health":
                                                if (method == "check")
                                                {
                                                    var status = ServerStatus.Status;
                                                    if (status == -1) {
                                                        context.Response.StatusCode = 503;
                                                        result = new { message = "Ведутся работы на сервере" };
                                                    } else {
                                                        result = new { message = "ОК" };
                                                    }
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
                                // Если у нас запрос обрабатывается через стандартный контроллерный интерфейс,
                                //   можем просто его обработать и получить свой результат.
                                //   На самом деле, это удобно и здорово повышает читаемость кода.
                                if (controllerObject != null)
                                {
                                    if (accessAllowed)
                                    {
                                        result = controllerObject.Handle(context, method);
                                    } else
                                    {
                                        context.Response.StatusCode = 401;
                                        result = new {
                                            success = false,
                                            message = $"Ваша роль \"{user?.role ?? "Не авторизован"}\" не позволяет использовать контроллер \"{controller}\"."
                                        };
                                    }
                                    
                                }

                                string resultText = JsonSerializer.Serialize(result);
                                await context.Response.WriteAsync(resultText);
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
            } else
            {
                Console.WriteLine("Завершение работы сервера...");
                return;
            }
            

            Console.WriteLine("Введите 'exit' или 'q', чтобы завершить сервер.");
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
                }
            }
        }
    } // Конец класса ServerApp

    public class ServerResponse
    {
        public int? Code { get; set; } = 200;
        public string? Message { get; set; }

        public dynamic? Data { get; set; }
    } // Конец класса ServerResponse

    public static class ServerStatus
    {
        private static volatile int _status;

        public static int Status
        {
            get => _status;
            set => _status = value;
        }
    }
} // Конец пространства имён ServerApp
