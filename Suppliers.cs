using Microsoft.AspNetCore.Http;
using ServerApp;
using System.Dynamic;
using System.Text.Json;
using System.Xml.Linq;

namespace Suppliers
{
    // Управление поставщиками
    public interface ISupplierService
    {
        bool AddSupplier(string name, string contactInfo);
        bool UpdateSupplier(int id, string name, string contactInfo);
        bool DeleteSupplier(int id);

        Supplier? GetSupplier(int id);
        List<Supplier> GetAllSuppliers();
    }

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

    public class SupplierController: IController
    {
        public const string Controller = "suppliers";
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        public object AddSupplier(string name, string contactInfo)
        {
            bool success = _supplierService.AddSupplier(name, contactInfo);
            return new {
                success,
                message = success ? "Поставщик добавлен" : "Ошибка добавления поставщика"
            };
        }

        public object UpdateSupplier(int id, string name, string contactInfo)
        {
            bool success = _supplierService.UpdateSupplier(id, name, contactInfo);
            return new {
                success,
                message = success ? "Поставщик обновлён" : "Ошибка обновления поставщика"
            };
        }

        public object DeleteSupplier(int id)
        {
            bool success = _supplierService.DeleteSupplier(id);
            return new
            {
                success,
                message = success ? "Поставщик удалён" : "Ошибка удаления поставщика"
            };
        }

        public object GetSupplier(int id)
        {
            var supplier = _supplierService.GetSupplier(id);
            return new {
                message = supplier == null ? "Поставщик не найден" : "Поставщик получен",
                data = supplier
            };
        }

        public object GetAllSuppliers()
        {
            return _supplierService.GetAllSuppliers();
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "add":
                    var name = context.Request.Query["name"];
                    var contactInfo = context.Request.Query["contact_info"];
                    result = this.AddSupplier(name, contactInfo);
                    break;
                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    name = context.Request.Query["name"];
                    contactInfo = context.Request.Query["contact_info"];
                    result = this.UpdateSupplier(id, name, contactInfo);
                    break;
                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.DeleteSupplier(id);
                    break;
                case "list":
                    result = this.GetAllSuppliers();
                    break;
                default:
                    context.Response.StatusCode = 404;
                    result = new { message = "Метод не найден" };
                    break;
            }
            return result;
        } 
        public static dynamic GetInterface()
        {
            dynamic interfaceData = new ExpandoObject();

            interfaceData.Suppliers = new
            {
                description = "Представление для управления поставщиками",
                controller = "suppliers",
                header = new
                {
                    id = "ID",
                    name = "Название",
                    contactInfo = "Контактная информация"
                },
                add = new
                {
                    name = new { text = "Название", type = "text" },
                    contact_info = new { text = "Контактная информация", type = "text" }
                },
                title = "поставщика",
                title_main = "Поставщики"
            };
            return interfaceData;
        } 

    } 

    public class Supplier
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? contactInfo { get; set; }

        public static Supplier FromDictionary(Dictionary<string, object?> row) {
            return new Supplier
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                contactInfo = Convert.ToString(row["contact_info"])
            };
        }
    }
}