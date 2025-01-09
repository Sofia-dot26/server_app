using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    // Управление пользователями
    public interface IUserService
    {
        int? AddUser(string login, string password, string role, out string error);
        bool UpdateUser(int id, string login, string password, string role, out string error);
        bool DeleteUser(int id);
        User? GetUser(int id);
        User? GetUser(string login);
        List<User> GetAllUsers();
    }
}
