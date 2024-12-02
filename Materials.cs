using Microsoft.AspNetCore.Http;
using ServerApp;

namespace Materials
{
    // ���������� �����������
    public interface IMaterialService
    {
        bool AddMaterial(string name, int quantity, string unit); 
        bool UpdateMaterial(int id, string name, int quantity, string unit);
        bool DeleteMaterial(int id);
        Material? GetMaterial(int id);
        List<Material> GetAllMaterials();
    } // ����� ���������� IMaterialService

    // ������ ��� ����������
    public class MaterialService : IMaterialService
    {
        public bool AddMaterial(string name, int quantity, string unit)
        {
            string sql = "INSERT INTO Materials (name, quantity, unit) VALUES (@name, @quantity, @unit)";
            var parameters = new Dictionary<string, object> {
                { "@name", name },
                { "@quantity", quantity },
                { "@unit", unit }
            };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public bool UpdateMaterial(int id, string name, int quantity, string unit)
        {
            string sql = "UPDATE Materials SET name = @name, quantity = @quantity, unit = @unit WHERE id = @id";
            var parameters = new Dictionary<string, object> {
                { "@id", id },
                { "@name", name },
                { "@quantity", quantity },
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

            return row != null ? new Material { 
                id = Convert.ToInt32(row["id"]),
                name = Convert.ToString(row["name"]),
                quantity = Convert.ToInt32(row["quantity"]),
                unit = Convert.ToString(row["unit"]),
            } : null;
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
                    quantity = Convert.ToInt32(row["quantity"]),
                    unit = Convert.ToString(row["unit"]),
                });
            }

            return materials;
        }
    } // ����� MaterialService


    public class MaterialController: IController
    {
        private readonly IMaterialService _materialService;

        public MaterialController(IMaterialService materialService)
        {
            _materialService = materialService;
        }

        public object AddMaterial(string name, int quantity, string unit) // ����� ������ AddMaterial
        {
            bool success = _materialService.AddMaterial(name, quantity, unit);
            return new
            {
                success,
                message = success ? "�������� ��������." : "������ ��� ���������� ���������."
            };
        }

        public object UpdateMaterial(int id, string name, int quantity, string unit) // ����� ������ UpdateMaterial
        {
            bool success = _materialService.UpdateMaterial(id, name, quantity, unit);
            return new
            {
                success,
                message = success ? "�������� �������." : "������ ��� ���������� ���������."
            };
        }

        public object DeleteMaterial(int id) // ����� ������ DeleteMaterial
        {
            bool success = _materialService.DeleteMaterial(id);
            return new
            {
                success,
                message = success ? "�������� �����." : "������ ��� �������� ���������."
            };
        }

        public object GetMaterial(int id)
        {
            var material = _materialService.GetMaterial(id);
            bool success = material != null;

            return new
            {
                success,
                message = success ? "�������� ������." : "�������� �� ������.",
                data = material
            };
        } // ����� ������ GetMaterial

        public object GetAllMaterials()
        {
            var materials = _materialService.GetAllMaterials();
            return new
            {
                success = true,
                data = materials
            };
        } // ����� ������ GetAllMaterials

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "add":
                    var name = context.Request.Query["name"];
                    var quantity = int.Parse(context.Request.Query["quantity"]);
                    var unit = context.Request.Query["unit"];
                    result = this.AddMaterial(name, quantity, unit);
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    name = context.Request.Query["name"];
                    quantity = int.Parse(context.Request.Query["quantity"]);
                    unit = context.Request.Query["unit"];
                    result = this.UpdateMaterial(id, name, quantity, unit);
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
                    result = new { message = "����� �� ������." };
                    break;
            }
            return result;
        } // ����� ������ Handle
    } // ����� MaterialController
      
    public class Material
    {
        public int id;
        public string? name;
        public int quantity;
        public string? unit;
    }
}