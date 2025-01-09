using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public class EquipmentService : IEquipmentService
    {
        public bool AddEquipment(string name, string description)
        {
            string sql = "INSERT INTO Equipment (name, description) VALUES (@name, @description)";
            var parameters = new Dictionary<string, object> {
                { "@name", name },
                { "@description", description }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public bool UpdateEquipment(int id, string name, string description)
        {
            string sql = "UPDATE Equipment SET name = @name, description = @description WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id },
                { "@name", name },
                { "@description", description }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public bool DeleteEquipment(int id)
        {
            string sql = "DELETE FROM Equipment WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } 

        public Equipment? GetEquipment(int id)
        {
            string sql = "SELECT * FROM Equipment WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            var row = (results.Count == 0) ? null : results[0];
            return row != null ? new Equipment
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                description = Convert.ToString(row["description"]),
            } : null; // Если запрос не вернул строк, возвращаем null
        } 

        public List<Equipment> GetAllEquipment()
        {
            string sql = "SELECT * FROM Equipment";
            var results = DatabaseHelper.ExecuteQuery(sql);

            var equipment = new List<Equipment>();
            foreach (var row in results)
            {
                equipment.Add(Equipment.FromDictionary(row));
            }

            return equipment;
        } 
    } 

}
