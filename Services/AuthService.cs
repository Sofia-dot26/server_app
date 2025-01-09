using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public class AuthService : IAuthService
    {
        public const string ROLE_ADMIN = "admin"; // Администратор
        public const string ROLE_DIRECTOR = "dir"; // Начальник подразделения
        public const string ROLE_ACCOUNTER = "acc"; // Учётчик

        public Session? Login(string username, string password, out User? user, out int? error, string ip = "127.0.0.1")
        {
            user = new UserService().GetUser(username);
            Session? result = null;
            error = null;
            if (user == null)
            {
                error = -1; // Юзер не существует
            }
            else
            {
                // Проверяем хэш пароля
                var passwordHash = UserService.getPasswordHash(password);

                if (user?.password_hash?.ToString() == passwordHash)
                {
                    result = this.CreateSession(Convert.ToInt32(user.id), ip);
                }
                else
                {
                    error = -2; // Неверный пароль
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

        public Session? CreateSession(int userId, string ip)
        {
            string sql = "INSERT INTO Sessions (user_id, created_at, expires_at, ip) VALUES (@user_id, NOW(), NOW() + INTERVAL '1 day', @ip) RETURNING id, user_id, created_at, expires_at, ip";
            var parameters = new Dictionary<string, object>
        {
            { "@user_id", userId },
            { "@ip", ip }
        };
            var result = DatabaseHelper.ExecuteQuery(sql, parameters);
            return result.Count > 0 ? Session.FromDictionary(result[0]) : null;
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

        public Session? GetSession(int sessionId) 
        {
            string sql = "SELECT * FROM Sessions WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", sessionId } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);

            return results.Count > 0 ? Session.FromDictionary(results[0]) : null;
        }

        public Session? GetSessionByUserId(int userId) 
        {
            string sql = "SELECT * FROM Sessions WHERE user_id = @user_id AND expires_at > NOW()";
            var parameters = new Dictionary<string, object> { { "@user_id", userId } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);

            return results.Count > 0 ? Session.FromDictionary(results[0]) : null;
        }

    }
}
