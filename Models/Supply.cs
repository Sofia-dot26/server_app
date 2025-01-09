using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Models
{
    public class Supply
    {
        public int id { get; set; }
        public int material_id { get; set; }
        public int supplier_id { get; set; }
        public int quantity { get; set; }
        public string? material_name { get; set; }
        public string? supplier_name { get; set; }
        public DateTime date { get; set; }

        public string date_human { get => this.date.ToString("dd.MM.yyyy"); }
        public string? unit { get; set; }
        public static Supply FromDictionary(Dictionary<string, object> row)
        {
            return new Supply
            {
                id = Convert.ToInt32(row["id"]),
                material_id = Convert.ToInt32(row["material_id"]),
                supplier_id = Convert.ToInt32(row["supplier_id"]),
                quantity = Convert.ToInt32(row["quantity"]),
                date = Convert.ToDateTime(row["date"]),
                material_name = row.ContainsKey("material_name") ? row["material_name"].ToString() : "",
                supplier_name = row.ContainsKey("supplier_name") ? row["supplier_name"].ToString() : "",
                unit = Convert.ToString(row["unit"])
            };
        }
    }
}
