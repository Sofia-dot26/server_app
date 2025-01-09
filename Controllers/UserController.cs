using AccountingServer.Models;
using AccountingServer.Services;
using Microsoft.AspNetCore.Http;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Controllers
{
    public class UserController : IController
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
            return new { success, message = success ? "Пользователь добавлен." : $"Ошибка при добавлении пользователя: {error}", id = addUserId };
        }

        public object Update(int id, string login, string password, string role)
        {
            bool success = _userService.UpdateUser(id, login, password, role, out string error);
            return new { success, message = success ? "Пользователь обновлён." : "Ошибка при обновлении пользователя." };

        }

        public object Delete(int id)
        {
            bool success = _userService.DeleteUser(id);
            return new { success, message = success ? "Пользователь удалён." : "Ошибка при удалении пользователя." };
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
                    result = id == user?.id ? new { message = "Роскомнадзор запрещает делать это" } : this.Delete(id);
                    break;
                case "list":
                    result = _userService.GetAllUsers();
                    break;

                default:
                    context.Response.StatusCode = 404;
                    result = new { message = "Метод не найден." };
                    break;
            }
            return result;
        }

        public static dynamic GetInterface()
        {
            dynamic interfaceData = new ExpandoObject();

            interfaceData.Users = new
            {
                description = "Представление для управления пользователями",
                controller = "users",
                header = new
                {
                    id = "ID",
                    login = "Логин",
                    password = "Пароль",
                    role_rus = "Роль"
                },
                add = new
                {
                    login = new { text = "Логин", type = "text" },
                    password = new { text = "Пароль", type = "password" },
                    role = new
                    {
                        text = "Роль",
                        type = "radio-images",
                        values = new
                        {
                            admin = "Администратор",
                            dir = "Начальник подразделения",
                            acc = "Учётчик"
                        }
                    },
                },
                title = "пользователя",
                title_main = "Пользователи"
            };
            return interfaceData;
        }
    }
}
