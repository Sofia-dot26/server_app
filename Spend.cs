using Materials;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerApp;
using Suppliers;
using System.Dynamic;

namespace Spend
{
    public interface ISpentMaterialService
    {
        bool AddSpend(int material_id, int quantity, DateTime date);
        bool UpdateSpend(int id, int material_id, int quantity, DateTime date);
        bool DeleteSpend(int id);
        Spend? GetSpend(int id);
        List<Spend> GetAllSpentMaterials();
    } 

    public class SpendService : ISpentMaterialService
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

    public class SpendController : IController
    {
        public const string Controller = "spend";
        private readonly ISpentMaterialService _SpendService;

        public SpendController(ISpentMaterialService SpendService)
        {
            _SpendService = SpendService;
        }

        public object AddSpend(int material_id, int quantity, DateTime date)
        {
            bool success = _SpendService.AddSpend(material_id, quantity, date);
            return new
            {
                success,
                message = success ? "Трата материалов добавлена." : "Ошибка при добавлении траты материалов."
            };
        } 

        public object UpdateSpend(int id, int material_id, int quantity, DateTime date)
        {
            bool success = _SpendService.UpdateSpend(id, material_id, quantity, date);
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

                    var quantity = ServerApp.ServerApp.getInt(context, "quantity") ?? 0;
                    if (quantity <= 0)
                    {
                        error += "Ошибка: количество должно быть больше нуля.";
                    }
                    var date = ServerApp.ServerApp.getDateTime(context, "date") ?? DateTime.Now;
                    result = string.IsNullOrEmpty(error) ? this.AddSpend(material_id, quantity, date) : new
                    {
                        success = false,
                        message = error
                    };
                    break;

                case "update":
                    var id = ServerApp.ServerApp.getInt(context, "id") ?? 0;
                    if (id <= 0)
                    {
                        error += "Ошибка: трата не выбрана";
                    }

                    material_id = ServerApp.ServerApp.getInt(context, "material_id") ?? 0;
                    material = material_id > 0 ? (new MaterialService()).GetMaterial(material_id) : null;
                    if (material == null)
                    {
                        error += "Ошибка: материал не выбран. ";
                    }

                    quantity = ServerApp.ServerApp.getInt(context, "quantity") ?? 0;
                    if (quantity <= 0)
                    {
                        error += "Ошибка: количество должно быть больше нуля.";
                    }

                    date = ServerApp.ServerApp.getDateTime(context, "date") ?? DateTime.Now;
                    result = string.IsNullOrEmpty(error) ? this.UpdateSpend(id, material_id, quantity, date) : new
                    {
                        success = false,
                        message = error
                    };
                    break;

                case "delete":
                    id = ServerApp.ServerApp.getInt(context, "id") ?? 0;
                    result = this.DeleteSpend(id);
                    break;

                case "get":
                    id = ServerApp.ServerApp.getInt(context, "id") ?? 0;
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
                    date_human = "Дата",
                    material_name = "Материал",
                    quantity = "Количество",
                    unit = "Единица"
                },
                add = new
                {
                    material_id = new { text = "Материал", type = "dictionary", controller = "Materials" },
                    quantity = new { text = "Количество", type = "number" },
                    date = new { text = "Дата", type = "date" },
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
        public int material_id { get; set; }
        public int quantity { get; set; }
        public DateTime date { get; set; }
        public string date_human { get => this.date.ToString("dd.MM.yyyy"); }

        public string? material_name { get; set; }       
        public string? unit { get; set; }
        public static Spend FromDictionary(Dictionary<string, object> row)
        {
            return new Spend
            {
                id = Convert.ToInt32(row["id"]),
                material_id = Convert.ToInt32(row["material_id"]),
                quantity = Convert.ToInt32(row["quantity"]),
                date = Convert.ToDateTime(row["date"]),
                material_name = row.ContainsKey("material_name") ? row["material_name"].ToString() : "",
                unit = Convert.ToString(row["unit"])
            };
        }
    }
}