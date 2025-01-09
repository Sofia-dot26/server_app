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
    public class MaterialController : IController
    {
        public const string Controller = "materials";
        private readonly IMaterialService _materialService;

        public MaterialController(IMaterialService materialService)
        {
            _materialService = materialService;
        }

        public object AddMaterial(string name,  string unit) 
        {
            bool success = _materialService.AddMaterial(name, unit);
            return new
            {
                success,
                message = success ? "Материал добавлен." : "Ошибка при добавлении материала."
            };
        }

        public object UpdateMaterial(int id, string name,  string unit) 
        {
            bool success = _materialService.UpdateMaterial(id, name,  unit);
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
                    var unit = context.Request.Query["unit"];
                    result = this.AddMaterial(name,  unit);
                    break;

                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    name = context.Request.Query["name"];
                    unit = context.Request.Query["unit"];
                    result = this.UpdateMaterial(id, name,  unit);
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
}
