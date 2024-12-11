using Microsoft.AspNetCore.Http;
using ServerApp;
using Suppliers;
using System.Dynamic;

namespace Equipment
{
    public interface IEquipmentService
    {
        bool AddEquipment(string name, string description);
        bool UpdateEquipment(int id, string name, string description);
        bool DeleteEquipment(int id);
        Equipment? GetEquipment(int id);
        List<Equipment> GetAllEquipment();
    }

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

    public class EquipmentController : IController
    {
        private readonly IEquipmentService _equipmentService;

        public EquipmentController(IEquipmentService equipmentService)
        {
            _equipmentService = equipmentService;
        }

        public object Handle(HttpContext context, string? method) 
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "add":
                    var name = context.Request.Query["name"];
                    var description = context.Request.Query["description"];
                    result = new
                    {
                        success = _equipmentService.AddEquipment(name, description),
                        message = "Оборудование добавлено."
                    };
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    name = context.Request.Query["name"];
                    description = context.Request.Query["description"];
                    result = new
                    {
                        success = _equipmentService.UpdateEquipment(id, name, description),
                        message = "Оборудование обновлено."
                    };
                    break;

                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = new
                    {
                        success = _equipmentService.DeleteEquipment(id),
                        message = "Оборудование удалено."
                    };
                    break;

                case "get":
                    id = int.Parse(context.Request.Query["id"]);
                    result = new
                    {
                        data = _equipmentService.GetEquipment(id),
                        message = "Оборудование получено."
                    };
                    break;

                case "list":
                    result = _equipmentService.GetAllEquipment();
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

            interfaceData.Equipment = new
            {
                description = "Представление для управления техникой",
                controller = "equipment",
                header = new
                {
                    id = "ID",
                    name = "Название",
                    description = "Описание"
                },
                add = new
                {
                    name = new { text = "Название", type = "text" },
                    description = new { text = "Описание", type = "text" }
                },
                title = "технику",
                title_main = "Техника"
            };
            return interfaceData;
        } 

    }


    public class Equipment
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public static Equipment FromDictionary(Dictionary<string, object?> row)
        {
            return new Equipment
            {
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                description = Convert.ToString(row["description"]),
            };
        }
    }
}