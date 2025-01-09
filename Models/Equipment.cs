using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Models
{
    public class Equipment
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }

        public static Equipment FromDictionary(Dictionary<string, object?> row)
        {
            return new Equipment
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                description = Convert.ToString(row["description"]),
            };
        }
    }
}
