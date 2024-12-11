using Microsoft.AspNetCore.Http;
using ServerApp;
using System.Dynamic;

namespace Materials
{
    // Управление материалами
    public interface IMaterialService
    {
        bool AddMaterial(string name, /*int quantity,*/ string unit);
        bool UpdateMaterial(int id, string name, /*int quantity,*/ string unit);
        bool DeleteMaterial(int id);
        Material? GetMaterial(int id);
        List<Material> GetAllMaterials();
    }

    // Сервис для материалов
    public class MaterialService : IMaterialService
    {
        public bool AddMaterial(string name,/*int quantity,*/ string unit)
        {
            //string sql = "INSERT INTO Materials (name, quantity, unit) VALUES (@name, @quantity, @unit)";
            string sql = "INSERT INTO Materials (name, unit) VALUES (@name, @unit) RETURNING id";
            var parameters = new Dictionary<string, object> {
                { "@name", name },
                //{ "@quantity", quantity },
                { "@unit", unit }
            };
            var result = DatabaseHelper.ExecuteQuery(sql, parameters);
            return result.Count > 0;
        }

        public bool UpdateMaterial(int id, string name, /*int quantity,*/ string unit)
        {
            //string sql = "UPDATE Materials SET name = @name, quantity = @quantity, unit = @unit WHERE id = @id";
            string sql = "UPDATE Materials SET name = @name, unit = @unit WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id },
                { "@name", name },
                //{ "@quantity", quantity },
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


    public class MaterialController : IController
    {
        private readonly IMaterialService _materialService;

        public MaterialController(IMaterialService materialService)
        {
            _materialService = materialService;
        }

        public object AddMaterial(string name, /*int quantity,*/ string unit) 
        {
            bool success = _materialService.AddMaterial(name, /*quantity,*/ unit);
            return new
            {
                success,
                message = success ? "Материал добавлен." : "Ошибка при добавлении материала."
            };
        }

        public object UpdateMaterial(int id, string name, /*int quantity,*/ string unit) 
        {
            bool success = _materialService.UpdateMaterial(id, name, /*quantity,*/ unit);
            return new
            {
                success,
                message = success ? "Материал обновлён." : "Ошибка при обновлении материала."
            };
        }

        public object DeleteMaterial(int id) 
        {
            bool success = _materialService.DeleteMaterial(id);
            return new
            {
                success,
                message = success ? "Материал удалён." : "Ошибка при удалении материала."
            };
        }

        public object GetMaterial(int id)
        {
            var material = _materialService.GetMaterial(id);
            bool success = material != null;

            return new
            {
                success,
                message = success ? "Материал найден." : "Материал не найден.",
                data = material
            };
        }

        public object GetAllMaterials()
        {
            return _materialService.GetAllMaterials();
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "add":
                    var name = context.Request.Query["name"];
                    //var quantity = int.Parse(context.Request.Query["quantity"]);
                    var unit = context.Request.Query["unit"];
                    result = this.AddMaterial(name, /*quantity,*/ unit);
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    name = context.Request.Query["name"];
                    //quantity = int.Parse(context.Request.Query["quantity"]);
                    unit = context.Request.Query["unit"];
                    result = this.UpdateMaterial(id, name, /*quantity,*/ unit);
                    break;

                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.DeleteMaterial(id);
                    break;

                case "get":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.GetMaterial(id);
                    break;

                case "list":
                    result = this.GetAllMaterials();
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
            interfaceData.Materials = new
            {
                description = "Представление для управления материалами",
                controller = "materials",
                header = new
                {
                    id = "ID",
                    name = "Название",
                    unit = "Единица"
                },
                add = new
                {
                    name = new { text = "Название", type = "text" },
                    unit = new { text = "Единица", type = "text" }
                },
                title = "материал",
                title_main = "Материалы"
            };
            return interfaceData;
        } 
    }

    public class Material
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? unit {  get; set; }

        public static Material FromDictionary(Dictionary<string, object?> row)
        {
            return new Material
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                unit = Convert.ToString(row["unit"]),
            };
        }
    }
}