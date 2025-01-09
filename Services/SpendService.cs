using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public class SpendService : ISpendMaterialService
    {
        public bool AddSpend(int material_id, int quantity, DateTime date)
        {
            string sql = "INSERT INTO SpentMaterials (material_id, quantity, date) VALUES (@material_id, @quantity, @date)";
            var parameters = new Dictionary<string, object> {
                { "@material_id", material_id },
                { "@quantity", quantity },
                { "@date", date }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public bool UpdateSpend(int id, int material_id, int quantity, DateTime date)
        {
            string sql = "UPDATE SpentMaterials SET material_id = @material_id, quantity = @quantity, date = @date WHERE id = @id";
            var parameters = new Dictionary<string, object>{
                { "@id", id },
                { "@material_id", material_id },
                { "@quantity", quantity },
                { "@date", date }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public bool DeleteSpend(int id)
        {
            string sql = "DELETE FROM SpentMaterials WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public Spend? GetSpend(int id)
        {
            string sql = "SELECT * FROM SpentMaterials WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            var row = (results.Count == 0) ? null : results[0];
            return row != null ? new Spend
            {
                id = Convert.ToInt32(row["id"]),
                material_id = Convert.ToInt32(row["material_id"]),
                quantity = Convert.ToInt32(row["quantity"]),
                date = Convert.ToDateTime(row["date"])
            } : null; // Если запрос не вернул строк, возвращаем null
        } 

        public List<Spend> GetAllSpentMaterials() 
        {
            string sql = @"
        SELECT 
            s.*,
            m.name AS material_name,
            m.unit AS unit
        FROM 
            SpentMaterials s
        LEFT JOIN 
            Materials m ON s.material_id = m.id";
            var results = DatabaseHelper.ExecuteQuery(sql);

            var SpentMaterials = new List<Spend>();
            foreach (var row in results)
            {
                SpentMaterials.Add(Spend.FromDictionary(row));
            }

            return SpentMaterials;
        } 

    } 
}
