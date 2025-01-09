using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Models
{
    public class Supplier
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? contactInfo { get; set; }

        public static Supplier FromDictionary(Dictionary<string, object?> row)
        {
            return new Supplier
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                contactInfo = Convert.ToString(row["contact_info"])
            };
        }
    }
}
