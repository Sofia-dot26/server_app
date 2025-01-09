using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public class SupplierService : ISupplierService
    {
        public bool AddSupplier(string name, string contactInfo)
        {
            string sql = "INSERT INTO Suppliers (name, contact_info) VALUES (@name, @contact_info)";
            var parameters = new Dictionary<string, object> {
                { "@name", name },
                { "@contact_info", contactInfo }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public bool UpdateSupplier(int id, string name, string contactInfo)
        {
            string sql = "UPDATE Suppliers SET name = @name, contact_info = @contact_info WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id },
                { "@name", name },
                { "@contact_info", contactInfo }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public bool DeleteSupplier(int id)
        {
            string sql = "DELETE FROM Suppliers WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public Supplier? GetSupplier(int id)
        {
            string sql = "SELECT * FROM Suppliers WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id }
            };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            var row = (results.Count == 0) ? null : results[0];
            return row != null ? new Supplier
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                contactInfo = Convert.ToString(row["contact_info"])
            } : null; // Если запрос не вернул строк, возвращаем null
        }

        public List<Supplier> GetAllSuppliers()
        {
            string sql = "SELECT * FROM Suppliers";
            var results = DatabaseHelper.ExecuteQuery(sql);

            var suppliers = new List<Supplier>();
            foreach (var row in results)
            {
                suppliers.Add(Supplier.FromDictionary(row));
            }

            return suppliers;
        }
    } 

}
