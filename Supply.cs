using Materials;
using Microsoft.AspNetCore.Http;
using ServerApp;
using Spend;
using Suppliers;
using System.Dynamic;

namespace Supply
{
    public interface ISupplyService
    {
        bool AddSupply(int material_id, int supplier_id, int quantity, DateTime date);
        bool UpdateSupply(int id, int material_id, int supplier_id, int quantity, DateTime date);
        bool DeleteSupply(int id);
        Supply? GetSupply(int id);
        List<Supply> GetAllSupplies();
    } 

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

    public class SupplyController : IController
    {
        public const string Controller = "supplies";
        private readonly ISupplyService _supplyService;

        public SupplyController(ISupplyService supplyService)
        {
            _supplyService = supplyService;
        }

        public object AddSupply(int material_id, int supplier_id, int quantity, DateTime date)
        {
            bool success = _supplyService.AddSupply(material_id, supplier_id, quantity, date);
            return new
            {
                success,
                message = success ? "Поставка добавлена." : "Ошибка при добавлении поставки."
            };
        } 

        public object UpdateSupply(int id, int material_id, int supplier_id, int quantity, DateTime date)
        {
            bool success = _supplyService.UpdateSupply(id, material_id, supplier_id, quantity, date);
            return new
            {
                success,
                message = success ? "Поставка обновлена." : "Ошибка при обновлении поставки."
            };
        } 

        public object DeleteSupply(int id)
        {
            bool success = _supplyService.DeleteSupply(id);
            return new
            {
                success,
                message = success ? "Поставка удалена." : "Ошибка при удалении поставки."
            };
        } 


        public Supply? GetSupply(int id)
        {
            return _supplyService.GetSupply(id);
        }

        public object GetAllSupplies()
        {
            return _supplyService.GetAllSupplies();
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic? result;
            string error = "";
            switch (method?.ToLower())
            {
                case "add":
                    int material_id = ServerApp.ServerApp.getInt(context, "material_id") ?? 0;
                    Material? material = material_id > 0 ? (new MaterialService()).GetMaterial(material_id) : null;
                    if (material == null)
                    {
                        error += "Ошибка: материал не выбран. ";
                    }

                    int supplier_id = ServerApp.ServerApp.getInt(context, "supplier_id") ?? 0;
                    Supplier? supplier = supplier_id > 0 ? (new SupplierService()).GetSupplier(supplier_id) : null;
                    if (supplier == null)
                    {
                        error += "Ошибка: поставщик не выбран. ";
                    }

                    int quantity = ServerApp.ServerApp.getInt(context, "quantity") ?? 0;
                    if (quantity <= 0)
                    {
                        error += "Ошибка: количество должно быть больше нуля.";
                    }

                    var date = ServerApp.ServerApp.getDateTime(context, "date") ?? DateTime.Now;
                    result = string.IsNullOrEmpty(error) ? this.AddSupply(material_id, supplier_id, quantity, date) : new
                    {
                        success = false,
                        message = error
                    };
                    break;

                case "update":
                    int id = ServerApp.ServerApp.getInt(context, "id") ?? 0;
                    if (id <= 0)
                    {
                        error += "Ошибка: поставка не выбрана";
                    }

                    material_id = int.Parse(context.Request.Query["material_id"]);
                    material = material_id > 0 ? (new MaterialService()).GetMaterial(material_id) : null;
                    if (material == null)
                    {
                        error += "Ошибка: материал не выбран. ";
                    }                    

                    supplier_id = ServerApp.ServerApp.getInt(context, "supplier_id") ?? 0;
                    supplier = supplier_id > 0 ? (new SupplierService()).GetSupplier(supplier_id) : null;
                    if (supplier == null)
                    {
                        error += "Ошибка: поставщик не выбран. ";
                    }

                    quantity = ServerApp.ServerApp.getInt(context, "quantity") ?? 0;
                    if (quantity <= 0)
                    {
                        error += "Ошибка: количество должно быть больше нуля.";
                    }

                    date = ServerApp.ServerApp.getDateTime(context, "date") ?? DateTime.Now;
                    result = string.IsNullOrEmpty(error) ? this.UpdateSupply(id, material_id, supplier_id, quantity, date) : new
                    {
                        success = false,
                        message = error
                    };
                    break;

                case "delete":
                    id = ServerApp.ServerApp.getInt(context, "id") ?? 0;
                    result = this.DeleteSupply(id);
                    break;

                case "get":
                    id = ServerApp.ServerApp.getInt(context, "id") ?? 0;
                    result = this.GetSupply(id);
                    if (result == null) result = new { message = "Поставка не найдена" };
                    break;

                case "list":
                    result = this.GetAllSupplies();
                    break;

                default:
                    context.Response.StatusCode = 404;
                    result = new { message = "Метод не найден." };
                    break;
            }
            return result;
        } 
        public static dynamic GetInterface()
        {
            dynamic interfaceData = new ExpandoObject();

            interfaceData.Supplies = new
            {
                description = "Представление для управления поставками",
                controller = "supplies",
                header = new
                {
                    id = "ID",
                    date_human = "Дата",
                    supplier_name = "Поставщик",
                    material_name = "Материал",
                    quantity = "Количество",
                    unit = "Единица"
                },
                add = new
                {
                    supplier_id = new { text = "Поставщик", type = "dictionary", controller = "Suppliers" },
                    material_id = new { text = "Материал", type = "dictionary", controller = "Materials" },
                    quantity = new { text = "Количество", type = "number" },
                    date = new { text = "Дата", type = "date", default_value = DateTime.Today.ToString("dd.MM.yyyy") },
                },
                title = "поставку",
                title_main = "Поставки"
            };
            return interfaceData;
        }
    } 

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
                material_id= Convert.ToInt32(row["material_id"]),
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