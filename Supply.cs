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
    } // ����� ���������� ISupplyService

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
        } // ����� ������ AddSupply

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
        } // ����� ������ UpdateSupply

        public bool DeleteSupply(int id)
        {
            string sql = "DELETE FROM Supplies WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        } // ����� ������ DeleteSupply

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
            } : null; // ���� ������ �� ������ �����, ���������� null
        } // ����� ������ GetSupply

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
        } // ����� ������ GetAllSupplies
    } // ����� SupplyService

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
                message = success ? "�������� ���������." : "������ ��� ���������� ��������."
            };
        } // ����� ������ AddSupply

        public object UpdateSupply(int id, int materialId, int supplierId, int quantity, DateTime date)
        {
            bool success = _supplyService.UpdateSupply(id, materialId, supplierId, quantity, date);
            return new
            {
                success,
                message = success ? "�������� ���������." : "������ ��� ���������� ��������."
            };
        } // ����� ������ UpdateSupply

        public object DeleteSupply(int id)
        {
            bool success = _supplyService.DeleteSupply(id);
            return new
            {
                success,
                message = success ? "�������� �������." : "������ ��� �������� ��������."
            };
        } // ����� ������ DeleteSupply


        public object GetSupply(int id)
        {
            var Supply = _supplyService.GetSupply(id);
            return new
            {
                message = Supply == null ? "�������� �� �������" : "�������� ��������",
                data = Supply
            };
        }

        public object GetAllSupplies()
        {
            var Supplies = _supplyService.GetAllSupplies();
            return new
            {
                message = Supplies.Count == 0 ? "������ ����" : "������ �������� �������",
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
                    result = new { message = "����� �� ������." };
                    break;
            }
            return result;
        } // ����� ������ Handle
    } // ����� SupplyController


    public class Supply
    {
        public int id;
        public int materialId;
        public int supplierId;
        public int quantity;
        public DateTime date;
    }
}