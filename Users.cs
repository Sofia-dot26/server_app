using Auth;
using Microsoft.AspNetCore.Http;
using ServerApp;
using System.Dynamic;
using System.Security.Cryptography;

namespace Users
{
    // ���������� ��������������
    public interface IUserService
    {
        int? AddUser(string login, string password, string role, out string error);
        bool UpdateUser(int id, string login, string password, string role, out string error);
        bool DeleteUser(int id);
        User? GetUser(int id);
        User? GetUser(string login);
        List<User> GetAllUsers();
    }


    // ������ ��� �������������
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
            error = (user != null ? "������������ ��� ����������. " : "") + (login == "" ? "����� �� ������. " : "") + (password == "" ? "������ �� ������. " : "") + (role == "" ? "���� �� �������." : "");
            // SQL-������ ��� ���������� ������������
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

            // �������� ������������� ������������ �� ID � ������
            if (login != null && login != "")
            {
                user_bylogin = GetUser(login);
            }
            if (id > 0)
            {
                user_byid = GetUser(id);
            }

            // ��������� �� ������������� ������������� � ���������� ������� ��� ID
           

            // ���������� ������, ���� ������������ �� �������
            error = (user_byid == null ? $"������������ � ID {id} �� ����������. " : "") +
                    ((user_bylogin != null && user_bylogin.id != id) ? $"������������ � ������� {login} ��� ����������. " : "");

            // ���� ������ �� �����, ��������� ����������
            if (!string.IsNullOrEmpty(error))
            {
                return false;
            }

            // ������ ������, ��������, ��� ��������� ��������� ����� ���� �������
            List<string> setClauses = new();
            var parameters = new Dictionary<string, object> { { "@id", id } };

            // ��������� ������ �� ���������, ������� �� ������
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

            // ���� ��� ��������� ������, �� ��������� ������
            if (setClauses.Count == 0)
            {
                error = "��� ����� ��� ����������";
                return false;
            }

            // ������ ��������� SQL ������
            string sql = $"UPDATE Users SET {string.Join(", ", setClauses)} WHERE id = @id";

            // ��������� ������
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

    public class UserController: IController
    {
        public const string Controller = "users";
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        public object Add(string login, string password, string role)
        {
            int? addUserId = _userService.AddUser(login, password, role, out string error);
            bool success = addUserId > 0;
            return new { success, message = success ? "������������ ��������." : $"������ ��� ���������� ������������: {error}", id = addUserId };
        }

        public object Update(int id, string login, string password, string role)
        {
            bool success = _userService.UpdateUser(id, login, password, role, out string error);
            return new { success, message = success ? "������������ �������." : "������ ��� ���������� ������������." };
               
        }

        public object Delete(int id)
        {
            bool success = _userService.DeleteUser(id);
            return new { success, message = success ? "������������ �����." : "������ ��� �������� ������������." };
        }

        public User? Get(int user_id)
        {
            return _userService.GetUser(user_id);
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            Session? session = (new AuthController(new AuthService())).GetSession(AuthController.GetSessionId(context) ?? 0);
            User? user = session == null ? null : (new UserController(new UserService())).Get(session.user_id);

            switch (method?.ToLower())
            {
                case "add":
                    var login = context.Request.Query["login"].ToString();
                    var password = context.Request.Query["password"].ToString();
                    var role = context.Request.Query["role"].ToString();
                    result = this.Add(login, password, role);
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    login = context.Request.Query["login"].ToString();
                    password = context.Request.Query["password"].ToString();
                    role = context.Request.Query["role"].ToString();
                    result = this.Update(id, login, password, role);
                    break;

                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = id == user?.id ? new { message = "������������ ��������� ������ ���" } : this.Delete(id);
                    break;
                case "list":
                    result = _userService.GetAllUsers();
                    break;

                default:
                    context.Response.StatusCode = 404;
                    result = new { message = "����� �� ������." };
                    break;
            }
            return result;
        } 

        public static dynamic GetInterface()
        {
            dynamic interfaceData = new ExpandoObject();
            interfaceData.Users = new
            {
                description = "������������� ��� ���������� ��������������",
                controller = "users",
                header = new
                {
                    id = "ID",
                    login = "�����",
                    password = "������",
                    role_rus = "����"
                },
                add = new
                {
                    login = new { text = "�����", type = "text" },
                    password = new { text = "������", type = "password" },
                    role = new
                    {
                        text = "����",
                        type = "radio-images",
                        values = new
                        {
                            admin = "�������������",
                            dir = "��������� �������������",
                            acc = "�������"
                        }
                    },
                },
                title = "������������",
                title_main = "������������"
            };
            return interfaceData;
        } 
    } 

    public class User
    {
        public int id { get; set; }
        public string? login { get; set; }
        public string? password_hash { get; set; }
        public string? role { get; set; }
        public string? role_rus { get => this.role switch { AuthService.ROLE_ADMIN => "�������������", AuthService.ROLE_DIRECTOR => "���������", AuthService.ROLE_ACCOUNTER => "�������", _ => "" }; }

        public static User FromDictionary(Dictionary<string, object?> row)
        {
            return new User
            {
                id = Convert.ToInt32(row["id"]),
                login = row["login"]?.ToString(),
                password_hash = row["password_hash"]?.ToString(),
                role = row["role"]?.ToString(),
            };
        }

        public bool isAdmin() {
            return this.role == "admin";
        }

        public bool isDirector() {
            return this.role == "dir";
        }

        public bool isAccounter() {
            return this.role == "acc";
        }
    }
}