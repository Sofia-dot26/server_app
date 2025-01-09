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
    public class ReportController : IController
    {
        public const string Controller = "reports";
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public dynamic DeleteReport(int id)
        {
            bool success = _reportService.DeleteReport(id);
            return new
            {
                success,
                message = success ? "Отчёт удалён." : "Ошибка при удалении отчёта."
            };
        }

        public object Handle(HttpContext context, string? method) 
        {
            dynamic result;

            AuthController authController = new(new AuthService());
            Session? session = authController.GetSession(AuthController.GetSessionId(context) ?? 0);
            User? user = session == null ? null : (new UserController(new UserService())).Get(session.user_id);
            int authorId = user?.id ?? 0;

            switch (method?.ToLower())
            {
                case "add":
                    string report_type = context.Request.Query.ContainsKey("report_type") ? context.Request.Query["report_type"] : "";
                    var period_start = context.Request.Query.ContainsKey("period_start") && !(string.IsNullOrEmpty(context.Request.Query["period_start"]))
                        ? DateTime.Parse(context.Request.Query["period_start"]) : DateTime.MinValue;
                    var period_end = context.Request.Query.ContainsKey("period_end") && !(string.IsNullOrEmpty(context.Request.Query["period_end"]))
                        ? DateTime.Parse(context.Request.Query["period_end"]) : DateTime.Now;
                    result = string.IsNullOrEmpty(report_type) ? new
                    {
                        success = false,
                        message = "Ошибка: выберите тип отчёта"
                    } : _reportService.AddReport(report_type, period_start, period_end, authorId);
                    break;
                case ReportService.REPORT_CONSUMPTION:
                    period_start = context.Request.Query.ContainsKey("start") ? DateTime.Parse(context.Request.Query["start"]) : DateTime.MinValue;
                    period_end = context.Request.Query.ContainsKey("period_end") ? DateTime.Parse(context.Request.Query["period_end"]) : DateTime.Now;
                    var previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateConsumptionReport(period_start, period_end, authorId, previewOnly);
                    break;

                case ReportService.REPORT_AVERAGE_CONSUMPTION:
                    var startMandatory = DateTime.Parse(context.Request.Query["period_start"]);
                    var endMandatory = DateTime.Parse(context.Request.Query["period_end"]);
                    previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateAverageConsumptionReport(startMandatory, endMandatory, authorId, previewOnly);
                    break;

                case ReportService.REPORT_REMAINING_MATERIALS:
                    previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateRemainingMaterialsReport(authorId, previewOnly);
                    break;

                case ReportService.REPORT_SUPPLIES:
                    previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateSuppliesReport(authorId, previewOnly);
                    break;

                case "delete":
                    int id = int.Parse(context.Request.Query["id"]);
                    result = this.DeleteReport(id);
                    break;

                case "list":
                    result = _reportService.GetAllReports();
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

            interfaceData.Reports = new
            {
                description = "Представление для просмотра отчетов",
                controller = "reports",
                header = new
                {
                    id = "ID",
                    date_human = "Сформирован",
                    author = "Автор",
                    type_human = "Тип отчёта",
                    date_start = "Дата, с",
                    date_end = "Дата, по",
                },
                add = new
                {
                    report_type = new
                    {
                        text = "Тип отчёта",
                        type = "radio-images",
                        values = new
                        {
                            consumption = "по расходу материалов",
                            average_consumption = "по среднему расходу",
                            remaining_materials = "по остаткам материалов",
                            supplies = "по поставкам"
                        }
                    },
                    period_start = new { text = "Дата, с", type = "date" },
                    period_end = new { text = "Дата, по", type = "date" },
                },
                viewMode = "table",
                noedit = true,
                title = "отчёт",
                title_main = "Отчёты"
            };
            return interfaceData;
        } 
    }
}
