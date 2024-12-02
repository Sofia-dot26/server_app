using Microsoft.AspNetCore.Http;
using ServerApp;
using System.Text.Json;
using Users;

namespace Auth
{
    public interface IAuthService
    {
        int Login(string username, string password, string ip = "127.0.0.1");
        bool Logout(int sessionId);
        int? CreateSession(int userId, string ip);
        bool ValidateSession(int sessionId);
        bool RemoveSession(int sessionId);
        bool RemoveExpiredSessions();
        Session? GetSession(int sessionId);
        Session? GetSessionByUserId(int userId);
    }

    public class AuthService : IAuthService
    {
        public int Login(string username, string password, string ip = "127.0.0.1")
        {
            var user = new UserService().GetUser(username);
            int result;
            if (user == null)
            {
                result = -1; // Юзер не существует
            } else {
                // Проверяем хэш пароля
                var passwordHash = UserService.getPasswordHash(password);

                if (user?.password_hash?.ToString() == passwordHash) {
                    result = this.CreateSession(Convert.ToInt32(user.id), ip) ?? 0;
                } else {
                    result = -2; // Неверный пароль
                }
            }

            return result;
        }

        public bool Logout(int sessionId)
        {
            return this.RemoveSession(sessionId);
        }

        public bool ValidateSession(int sessionId)
        {
            Session? session = this.GetSession(sessionId);
            return session != null && session.expires_at > DateTime.Now;
        }

        public int? CreateSession(int userId, string ip)
        {
            string sql = "INSERT INTO Sessions (user_id, created_at, expires_at, ip) VALUES (@user_id, NOW(), NOW() + INTERVAL '1 day', @ip) RETURNING id";
            var parameters = new Dictionary<string, object>
        {
            { "@user_id", userId },
            { "@ip", ip }
        };
            var result = DatabaseHelper.ExecuteQuery(sql, parameters);
            return result.Count > 0 ? Convert.ToInt32(result[0]["id"]) : null;
        }

        public bool RemoveSession(int sessionId)
        {
            string sql = "DELETE FROM Sessions WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", sessionId } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public bool RemoveExpiredSessions()
        {
            string sql = "DELETE FROM Sessions WHERE expires_at < NOW()";
            return DatabaseHelper.ExecuteNonQuery(sql, null);
        }

        public Session? GetSession(int sessionId) // Конец метода GetSession
        {
            string sql = "SELECT * FROM Sessions WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", sessionId } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);

            return results.Count > 0 ? Session.FromDictionary(results[0]) : null;
        }

        public Session? GetSessionByUserId(int userId) // Конец метода GetSessionByUserId
        {
            string sql = "SELECT * FROM Sessions WHERE user_id = @user_id AND expires_at > NOW()";
            var parameters = new Dictionary<string, object> { { "@user_id", userId } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);

            return results.Count > 0 ? Session.FromDictionary(results[0]) : null;
        }

    }

    public class AuthController : IController
    {

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        public object Login(string login, string password, string ip = "127.0.0.1")
        {
            int session_id = _authService.Login(login, password, ip);
            return new { 
                success = session_id > 0,
                message = login == "" 
                    ? "Логин не передан. Укажите его параметром login" 
                    : (
                        password == "" 
                            ? "Пароль не передан. Укажите его параметром password" 
                           : (session_id == -1 ? $"Пользователь {login} не существует" : (session_id == -2 ? "Неверный пароль" : ""))
                        ),
                session_id = session_id,
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
                    result = new {
                        user,
                        session,
                        valid = session?.isValid() ?? false,
                        user_role = user?.role,
                        allowed_controllers = user?.role switch { 
                            "admin" => "auth, health, reports, users",
                            "acc" => "auth, health, reports, materials, spend, supplies",
                            "dir" => "auth, health, reports, suppliers, equipment",
                            _ => "auth, health"
                        }
                    };
                    break;
                default:
                    context.Response.StatusCode = 404;
                    result = new { message = "Метод не найден." };
                    break;
            }
            return result;
        } // Конец метода Handle
    }

    public class Session
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime expires_at { get; set; }
        public string? ip { get; set; }

        // Конструктор для создания объекта из результата запроса
        public static Session FromDictionary(Dictionary<string, object?> row)
        {
            return new Session
            {
                id = Convert.ToInt32(row["id"]),
                user_id = Convert.ToInt32(row["user_id"]),
                created_at = Convert.ToDateTime(row["created_at"]),
                expires_at = Convert.ToDateTime(row["expires_at"]),
                ip = row["ip"]?.ToString()
            };
        }
        public bool isValid()
        {
            return this.expires_at > DateTime.Now;
        }
    } // Конец класса Session

    
}