using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public class SupplyService : ISupplyService
    {
        public bool AddSupply(int material_id, int supplier_id, int quantity, DateTime date)
        {
            string sql = "INSERT INTO Supplies (material_id, supplier_id, quantity, date) VALUES (@material_id, @supplier_id, @quantity, @date)";
            var parameters = new Dictionary<string, object> {
                { "@material_id", material_id },
                { "@supplier_id", supplier_id },
                { "@quantity", quantity },
                { "@date", date }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public bool UpdateSupply(int id, int material_id, int supplier_id, int quantity, DateTime date)
        {
            string sql = "UPDATE Supplies SET material_id = @material_id, supplier_id = @supplier_id, quantity = @quantity, date = @date WHERE id = @id";
            var parameters = new Dictionary<string, object>{
                { "@id", id },
                { "@material_id", material_id },
                { "@supplier_id", supplier_id },
                { "@quantity", quantity },
                { "@date", date }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public bool DeleteSupply(int id)
        {
            string sql = "DELETE FROM Supplies WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public Supply? GetSupply(int id)
        {
            string sql = "SELECT * FROM Supplies WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            var row = (results.Count == 0) ? null : results[0];
            return row != null ? new Supply
            {
                id = Convert.ToInt32(row["id"]),
                material_id = Convert.ToInt32(row["material_id"]),
                supplier_id = Convert.ToInt32(row["supplier_id"]),
                quantity = Convert.ToInt32(row["quantity"]),
                date = Convert.ToDateTime(row["date"])
            } : null; // Если запрос не вернул строк, возвращаем null
        } 

        public List<Supply> GetAllSupplies()
        {
            string sql = @"
        SELECT 
            s.*,
            m.name AS material_name,
            m.unit AS unit,
            sp.name AS supplier_name
        FROM 
            Supplies s
        LEFT JOIN 
            Materials m ON s.material_id = m.id
        LEFT JOIN 
            Suppliers sp ON s.supplier_id = sp.id";

            var results = DatabaseHelper.ExecuteQuery(sql);

            var supplies = new List<Supply>();
            foreach (var row in results)
            {
                // Заполняем объект Supply, включая дополнительные поля
                supplies.Add(Supply.FromDictionary(row));
            }

            return supplies;
        } 

    }
}
