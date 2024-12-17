using Microsoft.AspNetCore.Http;
using ServerApp;
using Spend;
using System.ComponentModel;
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
        SELECT s.*,
            COALESCE(m.name, '#NULL') AS material_name,
            COALESCE(sp.name, '#NULL') AS supplier_name
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
            switch (method?.ToLower())
            {
                case "add":
                    var material_id = int.Parse(context.Request.Query["material_id"]);
                    var supplier_id = int.Parse(context.Request.Query["supplier_id"]);
                    var quantity = int.Parse(context.Request.Query["quantity"]);
                    var date = DateTime.Parse(context.Request.Query["date"]);
                    result = this.AddSupply(material_id, supplier_id, quantity, date);
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    material_id = int.Parse(context.Request.Query["material_id"]);
                    supplier_id = int.Parse(context.Request.Query["supplier_id"]);
                    quantity = int.Parse(context.Request.Query["quantity"]);
                    date = DateTime.Parse(context.Request.Query["date"]);
                    result = this.UpdateSupply(id, material_id, supplier_id, quantity, date);
                    break;

                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.DeleteSupply(id);
                    break;

                case "get":
                    id = int.Parse(context.Request.Query["id"]);
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
                    date = "Дата",
                    supplier_name = "Поставщик",
                    material_name = "Материал",
                    quantity = "Количество",
                    unit = "Единица"
                },
                add = new
                {
                    date = new { text = "Дата", type = "date" },
                    supplier_id = new { text = "Поставщик", type = "dictionary", controller = "Suppliers" },
                    material_id = new { text = "Материал", type = "dictionary", controller = "Materials" },
                    quantity = new { text = "Количество", type = "number" },
                    unit = new { text = "Единица", type = "text" }
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
                supplier_name = row.ContainsKey("supplier_name") ? row["supplier_name"].ToString() : ""
            };
        }
    }
}