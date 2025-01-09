using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{

    // Сервис для пользователей
    public class UserService : IUserService
    {
        public static string getPasswordHash(string? password)
        {
            using var md5 = MD5.Create();
            return BitConverter.ToString(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password ?? ""))).Replace("-", "").ToLower();
        }
        public int? AddUser(string login, string password, string role, out string error)
        {
            User? user = null;
            if (login != "")
            {
                user = GetUser(login);
            }
            error = (user != null ? "Пользователь уже существует. " : "") + (login == "" ? "Логин не указан. " : "") + (password == "" ? "Пароль не указан. " : "") + (role == "" ? "Роль не указана." : "");
            // SQL-запрос для добавления пользователя
            string sql = "INSERT INTO Users (login, password_hash, role) VALUES (@login, @password_hash, @role) RETURNING id";
            var passwordHash = getPasswordHash(password);
            var parameters = new Dictionary<string, object>{
                { "@login", login },
                { "@password_hash", passwordHash },
                { "@role", role }
            };
            var result = error == "" ? DatabaseHelper.ExecuteQuery(sql, parameters) : null;

            return result?.Count > 0 ? Convert.ToInt32(result[0]["id"]) : null;
        }

        public bool UpdateUser(int id, string? login, string? password, string? role, out string error)
        {
            User? user_byid = null;
            User? user_bylogin = null;

            // Проверка существования пользователя по ID и логину
            if (login != null && login != "")
            {
                user_bylogin = GetUser(login);
            }
            if (id > 0)
            {
                user_byid = GetUser(id);
            }

            // Проверяем на существование пользователей с одинаковым логином или ID


            // Генерируем ошибку, если пользователи не найдены
            error = (user_byid == null ? $"Пользователь с ID {id} не существует. " : "") +
                    ((user_bylogin != null && user_bylogin.id != id) ? $"Пользователь с логином {login} уже существует. " : "");

            // Если ошибка не пуста, прерываем выполнение
            if (!string.IsNullOrEmpty(error))
            {
                return false;
            }

            // Строим запрос, учитывая, что некоторые параметры могут быть пустыми
            List<string> setClauses = new();
            var parameters = new Dictionary<string, object> { { "@id", id } };

            // Добавляем только те параметры, которые не пустые
            if (!string.IsNullOrEmpty(login))
            {
                setClauses.Add("login = @login");
                parameters.Add("@login", login);
            }

            if (!string.IsNullOrEmpty(password))
            {
                setClauses.Add("password_hash = MD5(@password)");
                parameters.Add("@password", password);
            }

            if (!string.IsNullOrEmpty(role))
            {
                setClauses.Add("role = @role");
                parameters.Add("@role", role);
            }

            // Если все параметры пустые, не обновляем ничего
            if (setClauses.Count == 0)
            {
                error = "Нет полей для обновления";
                return false;
            }

            // Строим финальный SQL запрос
            string sql = $"UPDATE Users SET {string.Join(", ", setClauses)} WHERE id = @id";

            // Выполняем запрос
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }


        public bool DeleteUser(int id)
        {
            string sql = "DELETE FROM Users WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public User? GetUser(int id)
        {
            string sql = "SELECT * FROM Users WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id }
            };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            return results.Count > 0 ? User.FromDictionary(results[0]) : null;
        }
        public User? GetUser(string login)
        {
            string sql = "SELECT * FROM Users WHERE LOWER(login) = LOWER(@login);";
            var parameters = new Dictionary<string, object> {
                { "@login", login }
            };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            return results.Count > 0 ? User.FromDictionary(results[0]) : null;
        }

        public List<User> GetAllUsers()
        {
            string sql = "SELECT * FROM Users";
            var results = DatabaseHelper.ExecuteQuery(sql);
            var users = new List<User>();
            foreach (var row in results)
            {
                users.Add(User.FromDictionary(row));
            }

            return users;
        }
    } 
}
