using AccountingServer.Models;
using AccountingServer.Services;
using Microsoft.AspNetCore.Http;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AccountingServer.Controllers
{
    public class AuthController : IController
    {
        public const string Controller = "auth";
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        public object Login(string login, string password, string ip = "127.0.0.1")
        {
            User? user;
            int? error = null;
            Session? session = _authService.Login(login, password, out user, out error, ip);
            int? session_id = session?.id;
            string name = user?.login ?? "товарищ";
            return new
            {
                success = session_id > 0,
                message = login == ""
                    ? "Логин не передан. Укажите его параметром login"
                    : (
                        password == ""
                            ? "Пароль не передан. Укажите его параметром password"
                           : (error == -1 ? $"Пользователь \"{login}\" не существует" : (error == -2 ? "Неверный пароль" : $"Добро пожаловать, {name}!"))
                        ),
                user,
                session,
                session_id,
                valid = session?.isValid() ?? false,
                user_role = user?.role,
                allowed_controllers = getAllowedControllers(user?.role ?? ""),
                allowed_views = getAllowedViews(user?.role ?? "")
            };
        }

        public object Logout(int sessionId)
        {
            bool logout_ok = _authService.Logout(sessionId);
            return new { success = logout_ok, message = logout_ok ? "Выход выполнен." : "Ошибка выхода" };
        }

        public bool Validate(int sessionId)
        {
            return _authService.ValidateSession(sessionId);
        }

        public Session? GetSession(int sessionId)
        {
            return _authService.GetSession(sessionId);
        }

        public static int? GetSessionId(HttpContext context)
        {
            int? session_id = null;

            // 1. Проверка заголовка "X-Session-ID"
            if (context.Request.Headers.TryGetValue("X-Session-ID", out var headerValue) && int.TryParse(headerValue, out var id))
            {
                session_id = id;
            }
            // 2. Проверка параметра в строке запроса (GET)
            else if (context.Request.Query.TryGetValue("session_id", out var queryValue) && int.TryParse(queryValue, out id))
            {
                session_id = id;
            }
            // 3. Проверка тела запроса (POST)
            else if (context.Request.Method == "POST" && context.Request.HasJsonContentType())
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = reader.ReadToEndAsync().Result;

                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
                        if (json != null && json.TryGetValue("session_id", out var sessionIdJson) && sessionIdJson.TryGetInt32(out id))
                        {
                            session_id = id;
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки десериализации
                    }
                }
            }

            return session_id;
        }

        public static Array getAllowedControllers(string user_role)
        {
            return user_role switch
            {
                AuthService.ROLE_ADMIN => new[] { AuthController.Controller, ReportController.Controller, UserController.Controller },
                AuthService.ROLE_ACCOUNTER => new[] { AuthController.Controller, ReportController.Controller, MaterialController.Controller, SpendController.Controller, SupplyController.Controller },
                AuthService.ROLE_DIRECTOR => new[] { AuthController.Controller, ReportController.Controller, SupplierController.Controller, EquipmentController.Controller },
                _ => new[] { AuthController.Controller }
            };
        }

        public static Array getAllowedViews(string user_role)
        {
            return user_role switch
            {
                AuthService.ROLE_ADMIN => new[] { "Reports", "Users" },
                AuthService.ROLE_ACCOUNTER => new[] { "Reports", "Materials", "Spends", "Supplies" },
                AuthService.ROLE_DIRECTOR => new[] { "Reports", "Suppliers", "Equipment" },
                _ => new[] { "Login" }
            };
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "login":
                    var login = context.Request.Query["login"];
                    var password = context.Request.Query["password"];
                    var ip = context.Connection.RemoteIpAddress?.ToString();
                    result = this.Login(login, password, ip ?? "unknown");
                    break;

                case "logout":
                    int? sessionId = GetSessionId(context);
                    result = this.Logout(sessionId ?? 0);
                    break;

                case "state":
                    Session? session = this.GetSession(GetSessionId(context) ?? 0);
                    User? user = session == null ? null : (new UserController(new UserService())).Get(session.user_id);
                    result = new
                    {
                        user,
                        session,
                        session_id = session?.id,
                        valid = session?.isValid() ?? false,
                        user_role = user?.role,
                        allowed_controllers = getAllowedControllers(user?.role ?? ""),
                        allowed_views = getAllowedViews(user?.role ?? "")
                    };

                    break;
                default:
                    context.Response.StatusCode = 404;
                    result = new { message = "Метод не найден." };
                    break;
            }
            return result;
        } 
    }
}
