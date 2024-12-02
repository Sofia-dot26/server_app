using Microsoft.AspNetCore.Http;
using ServerApp;
using Spend;

namespace Supply
{
    public interface ISupplyService
    {
        bool AddSupply(int materialId, int supplierId, int quantity, DateTime date);
        bool UpdateSupply(int id, int materialId, int supplierId, int quantity, DateTime date);
        bool DeleteSupply(int id);
        Supply? GetSupply(int id);
        List<Supply> GetAllSupplies();
    } // Конец интерфейса ISupplyService

    public class SupplyService : ISupplyService
    {
        public bool AddSupply(int materialId, int supplierId, int quantity, DateTime date)
        {
            string sql = "INSERT INTO Supplies (material_id, supplier_id, quantity, date) VALUES (@material_id, @supplier_id, @quantity, @date)";
            var parameters = new Dictionary<string, object> {
                { "@material_id", materialId },
                { "@supplier_id", supplierId },
                { "@quantity", quantity },
                { "@date", date }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } // Конец метода AddSupply

        public bool UpdateSupply(int id, int materialId, int supplierId, int quantity, DateTime date)
        {
            string sql = "UPDATE Supplies SET material_id = @material_id, supplier_id = @supplier_id, quantity = @quantity, date = @date WHERE id = @id";
            var parameters = new Dictionary<string, object>{
                { "@id", id },
                { "@material_id", materialId },
                { "@supplier_id", supplierId },
                { "@quantity", quantity },
                { "@date", date }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } // Конец метода UpdateSupply

        public bool DeleteSupply(int id)
        {
            string sql = "DELETE FROM Supplies WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } // Конец метода DeleteSupply

        public Supply? GetSupply(int id)
        {
            string sql = "SELECT * FROM Supplies WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            var results = DatabaseHelper.ExecuteQuery(sql, parameters);
            var row = (results.Count == 0) ? null : results[0];
            return row != null ? new Supply
            {
                id = Convert.ToInt32(row["id"]),
                materialId = Convert.ToInt32(row["material_id"]),
                supplierId = Convert.ToInt32(row["supplier_id"]),
                quantity = Convert.ToInt32(row["quantity"]),
                date = Convert.ToDateTime(row["date"])
            } : null; // Если запрос не вернул строк, возвращаем null
        } // Конец метода GetSupply

        public List<Supply> GetAllSupplies()
        {
            string sql = "SELECT * FROM Supplies";
            var results = DatabaseHelper.ExecuteQuery(sql);

            var supplies = new List<Supply>();
            foreach (var row in results)
            {
                supplies.Add(new Supply
                {
                    id = Convert.ToInt32(row["id"]),
                    materialId = Convert.ToInt32(row["material_id"]),
                    supplierId = Convert.ToInt32(row["supplier_id"]),
                    quantity = Convert.ToInt32(row["quantity"]),
                    date = Convert.ToDateTime(row["date"])
                });
            }

            return supplies;
        } // Конец метода GetAllSupplies
    } // Конец SupplyService

    public class SupplyController : IController
    {
        private readonly ISupplyService _supplyService;

        public SupplyController(ISupplyService supplyService)
        {
            _supplyService = supplyService;
        }

        public object AddSupply(int materialId, int supplierId, int quantity, DateTime date)
        {
            bool success = _supplyService.AddSupply(materialId, supplierId, quantity, date);
            return new
            {
                success,
                message = success ? "Поставка добавлена." : "Ошибка при добавлении поставки."
            };
        } // Конец метода AddSupply

        public object UpdateSupply(int id, int materialId, int supplierId, int quantity, DateTime date)
        {
            bool success = _supplyService.UpdateSupply(id, materialId, supplierId, quantity, date);
            return new
            {
                success,
                message = success ? "Поставка обновлена." : "Ошибка при обновлении поставки."
            };
        } // Конец метода UpdateSupply

        public object DeleteSupply(int id)
        {
            bool success = _supplyService.DeleteSupply(id);
            return new
            {
                success,
                message = success ? "Поставка удалена." : "Ошибка при удалении поставки."
            };
        } // Конец метода DeleteSupply


        public object GetSupply(int id)
        {
            var Supply = _supplyService.GetSupply(id);
            return new
            {
                message = Supply == null ? "Поставка не найдена" : "Поставка получена",
                data = Supply
            };
        }

        public object GetAllSupplies()
        {
            var Supplies = _supplyService.GetAllSupplies();
            return new
            {
                message = Supplies.Count == 0 ? "Список пуст" : "Список поставок получен",
                data = Supplies
            };
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "add":
                    var materialId = int.Parse(context.Request.Query["material_id"]);
                    var supplierId = int.Parse(context.Request.Query["supplier_id"]);
                    var quantity = int.Parse(context.Request.Query["quantity"]);
                    var date = DateTime.Parse(context.Request.Query["date"]);
                    result = this.AddSupply(materialId, supplierId, quantity, date);
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    materialId = int.Parse(context.Request.Query["material_id"]);
                    supplierId = int.Parse(context.Request.Query["supplier_id"]);
                    quantity = int.Parse(context.Request.Query["quantity"]);
                    date = DateTime.Parse(context.Request.Query["date"]);
                    result = this.UpdateSupply(id, materialId, supplierId, quantity, date);
                    break;

                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.DeleteSupply(id);
                    break;

                case "get":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.GetSupply(id);
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
        } // Конец метода Handle
    } // Конец SupplyController


    public class Supply
    {
        public int id;
        public int materialId;
        public int supplierId;
        public int quantity;
        public DateTime date;
    }
}