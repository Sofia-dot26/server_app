using AccountingServer.Models;
using AccountingServer.Services;
using Microsoft.AspNetCore.Http;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Controllers
{
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
}
