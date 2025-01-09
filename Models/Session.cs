using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Models
{
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
    } 
}
