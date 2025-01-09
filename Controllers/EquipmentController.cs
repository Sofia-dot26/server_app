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
    public class EquipmentController : IController
    {
        public const string Controller = "equipment";
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
                    string name = context.Request.Query["name"];
                    string description = context.Request.Query["description"];
                    bool success = _equipmentService.AddEquipment(name, description);
                    result = new
                    {
                        success,
                        message = success ? "Оборудование добавлено." : "Ошибка добавления"
                    };
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    name = context.Request.Query["name"];
                    description = context.Request.Query["description"];
                    success = _equipmentService.UpdateEquipment(id, name, description);
                    result = new
                    {
                        success,
                        message = success ? "Оборудование обновлено." : "Ошибка редактирования"
                    };
                    break;

                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    success = _equipmentService.DeleteEquipment(id);
                    result = new
                    {
                        success,
                        message = success ? "Оборудование удалено." : "Ошибка удаления"
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
}
