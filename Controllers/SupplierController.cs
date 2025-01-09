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
    public class SupplierController : IController
    {
        public const string Controller = "suppliers";
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        public object AddSupplier(string name, string contactInfo)
        {
            bool success = _supplierService.AddSupplier(name, contactInfo);
            return new
            {
                success,
                message = success ? "Поставщик добавлен" : "Ошибка добавления поставщика"
            };
        }

        public object UpdateSupplier(int id, string name, string contactInfo)
        {
            bool success = _supplierService.UpdateSupplier(id, name, contactInfo);
            return new
            {
                success,
                message = success ? "Поставщик обновлён" : "Ошибка обновления поставщика"
            };
        }

        public object DeleteSupplier(int id)
        {
            bool success = _supplierService.DeleteSupplier(id);
            return new
            {
                success,
                message = success ? "Поставщик удалён" : "Ошибка удаления поставщика"
            };
        }

        public object GetSupplier(int id)
        {
            var supplier = _supplierService.GetSupplier(id);
            return new
            {
                message = supplier == null ? "Поставщик не найден" : "Поставщик получен",
                data = supplier
            };
        }

        public object GetAllSuppliers()
        {
            return _supplierService.GetAllSuppliers();
        }

        public object Handle(HttpContext context, string? method)
        {
            dynamic result;
            switch (method?.ToLower())
            {
                case "add":
                    var name = context.Request.Query["name"];
                    var contactInfo = context.Request.Query["contact_info"];
                    result = this.AddSupplier(name, contactInfo);
                    break;
                case "update":
                    var id = int.Parse(context.Request.Query["id"]);
                    name = context.Request.Query["name"];
                    contactInfo = context.Request.Query["contact_info"];
                    result = this.UpdateSupplier(id, name, contactInfo);
                    break;
                case "delete":
                    id = int.Parse(context.Request.Query["id"]);
                    result = this.DeleteSupplier(id);
                    break;
                case "list":
                    result = this.GetAllSuppliers();
                    break;
                default:
                    context.Response.StatusCode = 404;
                    result = new { message = "Метод не найден" };
                    break;
            }
            return result;
        }
        public static dynamic GetInterface()
        {
            dynamic interfaceData = new ExpandoObject();

            interfaceData.Suppliers = new
            {
                description = "Представление для управления поставщиками",
                controller = "suppliers",
                header = new
                {
                    id = "ID",
                    name = "Название",
                    contactInfo = "Контактная информация"
                },
                add = new
                {
                    name = new { text = "Название", type = "text" },
                    contact_info = new { text = "Контактная информация", type = "text" }
                },
                title = "поставщика",
                title_main = "Поставщики"
            };
            return interfaceData;
        }

    }
}
