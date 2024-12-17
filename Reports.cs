using Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServerApp;
using System.Dynamic;
using System.Text.Json;
using Users;

namespace Reports
{
    public interface IReportService
    {
        dynamic AddReport(string reportType, DateTime period_start, DateTime period_end, int authorId = 0);
        dynamic GenerateConsumptionReport(DateTime? start, DateTime? end, int authorId, bool previewOnly);
        dynamic GenerateAverageConsumptionReport(DateTime start, DateTime end, int authorId, bool previewOnly);
        dynamic GenerateRemainingMaterialsReport(int authorId, bool previewOnly);
        dynamic GenerateSuppliesReport(int authorId, bool previewOnly);

        bool DeleteReport(int id);
        List<Report> GetAllReports();
    }

    public class ReportService : IReportService
    {
        public const string FIELD_ID = "id";
        public const string REPORT_CONSUMPTION = "consumption";
        public const string REPORT_AVERAGE_CONSUMPTION = "average_consumption";
        public const string REPORT_REMAINING_MATERIALS = "remaining_materials";
        public const string REPORT_SUPPLIES = "supplies";

        public dynamic AddReport(string reportType, DateTime period_start, DateTime period_end, int authorId = 0)
        {
            dynamic? report = reportType switch
            {
                REPORT_CONSUMPTION => GenerateConsumptionReport(period_start, period_end, authorId, false),
                REPORT_AVERAGE_CONSUMPTION => GenerateAverageConsumptionReport(period_start, period_end, authorId, false),
                REPORT_REMAINING_MATERIALS => GenerateRemainingMaterialsReport(authorId, false),
                REPORT_SUPPLIES => GenerateSuppliesReport(authorId, false),
                _ => null
            };
            string reportTypeRus = reportType switch
            {
                REPORT_CONSUMPTION => "по расходу материалов",
                REPORT_AVERAGE_CONSUMPTION => "по среднему расходу материалов",
                REPORT_REMAINING_MATERIALS => "по остатку материалов",
                REPORT_SUPPLIES => "по поставкам",
                _ => reportType
            };
            bool success = report != null;

            return new
            {
                success,
                message = success ? $"Отчёт {reportTypeRus} сформирован" : $"Ошибка формирования отчёта {reportTypeRus}",
                report
            };
        }

        public bool DeleteReport(int id)
        {
            string sql = "DELETE FROM Reports WHERE id = @id";
            var parameters = new Dictionary<string, object> { { "@id", id } };
            return DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public static string today()
        {
            return DateTime.Today.ToString("dd.MM.yyyy");
        }

        public dynamic GenerateConsumptionReport(DateTime? start, DateTime? end, int authorId, bool previewOnly)
        {
            string title = "Отчёт по расходу материалов";
            string period = $"с {start?.ToString("dd.MM.yyyy") ?? "начала учёта"} по {end?.ToString("dd.MM.yyyy") ?? "конец учёта"}";
            string query = "SELECT m.name, SUM(sm.quantity) AS total_quantity, m.unit " +
                           "FROM SpentMaterials sm " +
                           "JOIN Materials m ON sm.material_id = m.id " +
                           "WHERE (@start IS NULL OR sm.date >= @start) AND (@end IS NULL OR sm.date <= @end) " +
                           "GROUP BY m.name, m.unit";

            var parameters = new Dictionary<string, object>
            {
                { "@start", start.HasValue ? start.Value : DBNull.Value },
                { "@end", end.HasValue ? end.Value : DBNull.Value }
            };

            var results = DatabaseHelper.ExecuteQuery(query, parameters);

            var lines = results.Select(row => new {
                name = row["name"],
                total_quantity = row["total_quantity"],
                unit = row["unit"]
            });

            dynamic result = new
            {
                type = "table",
                report_type = REPORT_CONSUMPTION,
                legend = $"{title}<br>\nЗа период {period}",
                headers = new
                {
                    name = "Материал",
                    total_quantity = "Сумма расхода",
                    unit = "Единица"
                },
                values = lines
            };

            if (!previewOnly)
            {
                SaveReport(REPORT_CONSUMPTION, start, end, JsonSerializer.Serialize(result), authorId);
            }

            return result;
        }

        public dynamic GenerateAverageConsumptionReport(DateTime start, DateTime end, int authorId, bool previewOnly)
        {
            string title = "Отчёт по среднему расходу материалов";
            string period = $"с {start.ToString("dd.MM.yyyy") ?? "начала учёта"} по {end.ToString("dd.MM.yyyy") ?? "конец учёта"}";

            // Количество дней в периоде
            int daysInPeriod = (end - start).Days + 1;

            string query = @"
        SELECT 
            m.name,
            SUM(sm.quantity) / @days_in_period AS average_daily,
            m.unit
        FROM 
            SpentMaterials sm
        JOIN 
            Materials m ON sm.material_id = m.id
        WHERE 
            sm.date >= @start AND sm.date <= @end
        GROUP BY 
            m.name, m.unit";

            var parameters = new Dictionary<string, object>
            {
                { "@start", start },
                { "@end", end },
                { "@days_in_period", daysInPeriod }
            };

            var results = DatabaseHelper.ExecuteQuery(query, parameters);

            var lines = results.Select(row => new {
                name = row["name"],
                average_daily = row["average_daily"],
                unit = row["unit"]
            });

            dynamic result = new
            {
                type = "table",
                report_type = REPORT_AVERAGE_CONSUMPTION,
                legend = $"{title} <br>\nЗа период {period}",
                headers = new
                {
                    name = "Материал",
                    average_daily = "Средний расход",
                    unit = "Единица"
                },
                values = lines
            };

            // Сохраняем отчёт, если не превью
            if (!previewOnly)
            {
                SaveReport(REPORT_AVERAGE_CONSUMPTION, start, end, JsonSerializer.Serialize(result), authorId);
            }

            return result;
        }


        public dynamic GenerateRemainingMaterialsReport(int authorId, bool previewOnly)
        {
            string title = "Отчёт по остаткам материалов";
            //string header = "Название материала\tСостояние";

            string query = @"SELECT m.id, m.name, COALESCE(supply_total.quantity, 0) - COALESCE(spent_total.quantity, 0) AS balance
FROM Materials m
LEFT JOIN 
    (SELECT material_id, SUM(quantity) AS quantity FROM Supplies 
     GROUP BY material_id) AS supply_total ON supply_total.material_id = m.id
LEFT JOIN 
    (SELECT material_id, SUM(quantity) AS quantity FROM SpentMaterials 
     GROUP BY material_id) AS spent_total ON spent_total.material_id = m.id
ORDER BY m.id;";

            var results = DatabaseHelper.ExecuteQuery(query);

            var lines = results.Select(row => new {
                name = row["name"],
                balance = row["balance"],
                status = int.Parse(row["balance"]?.ToString() ?? "0") > 0 ? "<div class=\"green\">В наличии</div>" : "Израсходован"
            });

            dynamic result = new
            {
                type = "table",
                report_type = REPORT_REMAINING_MATERIALS,
                legend = title,
                headers = new
                {
                    name = "Материал",
                    balance = "Остаток",
                    status = "Статус"
                },
                values = lines
            };

            if (!previewOnly)
            {
                SaveReport(REPORT_REMAINING_MATERIALS, null, null, JsonSerializer.Serialize(result), authorId);
            }

            return result;
        }

        public dynamic GenerateSuppliesReport(int authorId, bool previewOnly)
        {
            string title = "Отчёт по поставкам";

            string query = "SELECT su.id, su.date, s.name AS supplier, m.name AS material, su.quantity, m.unit " +
                           "FROM Supplies su " +
                           "JOIN Suppliers s ON su.supplier_id = s.id " +
                           "JOIN Materials m ON su.material_id = m.id";

            var results = DatabaseHelper.ExecuteQuery(query);

            var lines = results.Select(row => new {
                id = row["id"],
                date = DateTime.Parse(row["date"]?.ToString() ?? "").ToString("dd.MM.yyyy"),
                supplier = row["supplier"],
                material = row["material"],
                quantity = row["quantity"],
                unit = row["unit"],
            });


            dynamic result = new
            {
                type = "table",
                report_type = REPORT_SUPPLIES,
                legend = title,
                headers = new
                {
                    id = "ID",
                    date = "Дата поставки",
                    supplier = "Поставщик",
                    material = "Материал",
                    quantity = "Количество",
                    unit = "Единица"
                },
                values = lines
            };

            if (!previewOnly)
            {
                SaveReport(REPORT_SUPPLIES, null, null, JsonSerializer.Serialize(result), authorId);
            }

            return result;
        }

        private int? SaveReport(string reportType, DateTime? start, DateTime? end, string content, int authorId)
        {
            string sql = "INSERT INTO Reports (report_type, report_date, author_id, period_start, period_end, content) " +
                "VALUES (@report_type, @report_date, @author_id, @period_start, @period_end, @content) RETURNING id";
            var parameters = new Dictionary<string, object>
            {
                { "@report_type", reportType },
                { "@report_date", DateTime.Now },
                { "@author_id", authorId },
                { "@period_start", start.HasValue ? start.Value : DBNull.Value },
                { "@period_end", end.HasValue ? end.Value : DBNull.Value },
                { "@content", content }
            };

            var results = DatabaseHelper.ExecuteQuery(sql, parameters);

            return results.Count > 0 ? int.Parse(results[0][ReportService.FIELD_ID]?.ToString() ?? "0") : null;
        }

        public List<Report> GetAllReports()
        {
            string sql = "SELECT *, u.login as author, r.id as report_id FROM Reports r LEFT JOIN Users u ON r.author_id=u.id";
            var results = DatabaseHelper.ExecuteQuery(sql);

            return results.Select(Report.FromDictionary).ToList();
        }

    }

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

    public class Report
    {
        public int id { get; set; }
        public string? report_type { get; set; }

        public string type_human
        {
            get => report_type switch
            {
                ReportService.REPORT_CONSUMPTION => "Расход",
                ReportService.REPORT_AVERAGE_CONSUMPTION => "Расход средний",
                ReportService.REPORT_REMAINING_MATERIALS => "Остатки",
                ReportService.REPORT_SUPPLIES => "Поставки",
                _ => report_type ?? "н/д"
            };
        }
        public DateTime? report_date { get; set; }
        public string date_human
        {
            get => report_date == null ? "" :
                (report_date?.ToString("dd.MM.yyyy") == ReportService.today()
                    ? "сегодня" :
                        (report_date == DateTime.MinValue ? "нет" : report_date?.ToString("dd.MM.yyyy") ?? ""));
        }
        public string author { get; set; }
        public DateTime? period_start { get; set; }
        public string date_start
        {
            get => period_start == null ? "" :
                (period_start?.ToString("dd.MM.yyyy") == ReportService.today()
                    ? "сегодня" :
                        (period_start == DateTime.MinValue ? "нет" : period_start?.ToString("dd.MM.yyyy") ?? ""));
        }
        public DateTime? period_end { get; set; }
        public string date_end
        {
            get => period_start == null ? "" :
                (period_end?.ToString("dd.MM.yyyy") == ReportService.today()
                    ? "сегодня" :
                        (period_end == DateTime.MinValue ? "нет" : period_end?.ToString("dd.MM.yyyy") ?? ""));
        }
        public string? content { get; set; }

        public dynamic? data { get; set; }

        public static Report FromDictionary(Dictionary<string, object?> row) => new Report
        {
            id = Convert.ToInt32(row["report_id"]),
            report_type = row["report_type"]?.ToString(),
            report_date = row["report_date"] != DBNull.Value ? Convert.ToDateTime(row["report_date"]) : (DateTime?)null,
            author = row["author"]?.ToString() ?? "",
            period_start = row["period_start"] != DBNull.Value ? Convert.ToDateTime(row["period_start"]) : (DateTime?)null,
            period_end = row["period_end"] != DBNull.Value ? Convert.ToDateTime(row["period_end"]) : (DateTime?)null,
            content = row["content"]?.ToString(),
            data = JsonDocument.Parse(row["content"]?.ToString() ?? "{}")
        };
    }
}