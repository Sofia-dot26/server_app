using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerApp;
using System.Dynamic;

namespace Spend
{
    public interface ISpentMaterialService
    {
        bool AddSpend(int materialId, int quantity, DateTime date);
        bool UpdateSpend(int id, int materialId, int quantity, DateTime date);
        bool DeleteSpend(int id);
        Spend? GetSpend(int id);
        List<Spend> GetAllSpentMaterials();
    }

    public class SpendService : ISpentMaterialService
    {
        public bool AddSpend(int materialId, int quantity, DateTime date)
        {
            string sql = "INSERT INTO SpentMaterials (material_id, quantity, date) VALUES (@material_id, @supplier_id, @quantity, @date)";
            var parameters = new Dictionary<string, object> {
                { "@material_id", materialId },
                { "@quantity", quantity },
                { "@date", date }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public bool UpdateSpend(int id, int materialId, int quantity, DateTime date)
        {
            string sql = "UPDATE SpentMaterials SET material_id = @material_id, quantity = @quantity, date = @date WHERE id = @id";
            var parameters = new Dictionary<string, object>{
                { "@id", id },
                { "@material_id", materialId },
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
                materialId = Convert.ToInt32(row["material_id"]),
                quantity = Convert.ToInt32(row["quantity"]),
                date = Convert.ToDateTime(row["date"])
            } : null; // Если запрос не вернул строк, возвращаем null
        } 

        public List<Spend> GetAllSpentMaterials() 
        {
            string sql = "SELECT * FROM SpentMaterials";
            var results = DatabaseHelper.ExecuteQuery(sql);

            var SpentMaterials = new List<Spend>();
            foreach (var row in results)
            {
                SpentMaterials.Add(new Spend
                {
                    id = Convert.ToInt32(row["id"]),
                    materialId = Convert.ToInt32(row["material_id"]),
                    quantity = Convert.ToInt32(row["quantity"]),
                    date = Convert.ToDateTime(row["date"])
                });
            }

            return SpentMaterials;
        } 

    } 

    public class SpendController : IController
    {
        private readonly ISpentMaterialService _SpendService;

        public SpendController(ISpentMaterialService SpendService)
        {
            _SpendService = SpendService;
        }

        public object AddSpend(int materialId, int quantity, DateTime date)
        {
            bool success = _SpendService.AddSpend(materialId, quantity, date);
            return new
            {
                success,
                message = success ? "Трата материалов добавлена." : "Ошибка при добавлении траты материалов."
            };
        } 

        public object UpdateSpend(int id, int materialId, int quantity, DateTime date)
        {
            bool success = _SpendService.UpdateSpend(id, materialId, quantity, date);
            return new
            {
                success,
                message = success ? "Трата обновлена." : "Ошибка при обновлении траты."
            };
        } 

        public object DeleteSpend(int id)
        {
            bool success = _SpendService.DeleteSpend(id);
            return new
            {
                success,
                message = success ? "Трата удалена." : "Ошибка при удалении траты."
            };
        } 


        public object GetSpend(int id)
        {
            var Spend = _SpendService.GetSpend(id);
            return new {
                message = Spend == null ? "Трата не найдена" : "Трата получена",
                data = Spend
            };
        }

        public object GetAllSpentMaterials()
        {
            return _SpendService.GetAllSpentMaterials();
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "add":
                    var materialId = int.Parse(context.Request.Query["material_id"]);
                    var quantity = int.Parse(context.Request.Query["quantity"]);
                    var date = DateTime.Parse(context.Request.Query["date"]);
                    result = this.AddSpend(materialId, quantity, date);
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    materialId = int.Parse(context.Request.Query["material_id"]);
                    quantity = int.Parse(context.Request.Query["quantity"]);
                    date = DateTime.Parse(context.Request.Query["date"]);
                    result = this.UpdateSpend(id, materialId, quantity, date);
                    break;

                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.DeleteSpend(id);
                    break;

                case "get":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.GetSpend(id);
                    break;

                case "list":
                    result = this.GetAllSpentMaterials();
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

            interfaceData.Spends = new
            {
                description = "Представление для управления тратами",
                controller = "spend",
                header = new
                {
                    id = "ID",
                    date = "Дата",
                    material_name = "Материал",
                    quantity = "Количество",
                    unit = "Единица"
                },
                add = new
                {
                    date = new { text = "Дата", type = "date" },
                    material_name = new { text = "Материал", type = "text" },
                    quantity = new { text = "Количество", type = "number" },
                    unit = new { text = "Единица", type = "text" }
                },
                title = "трату",
                title_main = "Траты"
            };
            return interfaceData;
        } 

    } 


    public class Spend
    {
        public int id { get; set; }
        public int materialId { get; set; }
        public int supplierId { get; set; }
        public int quantity { get; set; }
        public DateTime date { get; set; }
    }
}