using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    // Сервис для материалов
    public class MaterialService : IMaterialService
    {
        public bool AddMaterial(string name, string unit)
        {
            string sql = "INSERT INTO Materials (name, unit) VALUES (@name, @unit) RETURNING id";
            var parameters = new Dictionary<string, object> {
                { "@name", name },
                { "@unit", unit }
            };
            var result = DatabaseHelper.ExecuteQuery(sql, parameters);
            return result.Count > 0;
        }

        public bool UpdateMaterial(int id, string name, string unit)
        {
            string sql = "UPDATE Materials SET name = @name, unit = @unit WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id },
                { "@name", name },
                { "@unit", unit }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public bool DeleteMaterial(int id)
        {
            string sql = "DELETE FROM Materials WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public Material? GetMaterial(int id)
        {
            string sql = "SELECT * FROM Materials WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            var row = (results.Count == 0) ? null : results[0];

            return row != null ? Material.FromDictionary(results[0]) : null;
        }

        public List<Material> GetAllMaterials()
        {
            string sql = "SELECT * FROM Materials";
            var results = DatabaseHelper.ExecuteQuery(sql);
            var materials = new List<Material>();
            foreach (var row in results)
            {
                materials.Add(new Material
                {
                    id = Convert.ToInt32(row["id"]),
                    name = Convert.ToString(row["name"]),
                    unit = Convert.ToString(row["unit"]),
                });
            }

            return materials;
        }
    } 
}
