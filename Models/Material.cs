using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Models
{
    public class Material
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? unit { get; set; }

        public static Material FromDictionary(Dictionary<string, object?> row)
        {
            return new Material
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                unit = Convert.ToString(row["unit"]),
            };
        }
    }
}
