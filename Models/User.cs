using AccountingServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Models
{
    public class User
    {
        public int id { get; set; }
        public string? login { get; set; }
        public string? password_hash { get; set; }
        public string? role { get; set; }
        public string? role_rus { get => this.role switch { AuthService.ROLE_ADMIN => "Администратор", AuthService.ROLE_DIRECTOR => "Начальник", AuthService.ROLE_ACCOUNTER => "Учётчик", _ => "" }; }

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

        public bool isAdmin()
        {
            return this.role == "admin";
        }

        public bool isDirector()
        {
            return this.role == "dir";
        }

        public bool isAccounter()
        {
            return this.role == "acc";
        }
    }
}
