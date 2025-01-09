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
    public class SpendController : IController
    {
        public const string Controller = "spend";
        private readonly ISpendMaterialService _SpendService;

        public SpendController(ISpendMaterialService SpendService)
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
            return new
            {
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
}
