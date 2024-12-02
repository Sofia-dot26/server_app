using Microsoft.AspNetCore.Http;
using ServerApp;

namespace Reports
{
    public interface IReportService
    {
        string GenerateConsumptionReport(DateTime? start, DateTime? end, int authorId, bool previewOnly); // Конец метода GenerateConsumptionReport
        string GenerateAverageConsumptionReport(DateTime start, DateTime end, int authorId, bool previewOnly); // Конец метода GenerateAverageConsumptionReport
        string GenerateRemainingMaterialsReport(int authorId, bool previewOnly); // Конец метода GenerateRemainingMaterialsReport
        string GenerateSuppliesReport(int authorId, bool previewOnly); // Конец метода GenerateSuppliesReport
        List<Report> GetAllReports(); // Конец метода GetAllReports
    }

    public class ReportService : IReportService
    {
        public string GenerateConsumptionReport(DateTime? start, DateTime? end, int authorId, bool previewOnly) // Конец метода GenerateConsumptionReport
        {
            string title = "Отчёт по расходу материалов";
            string period = $"{start?.ToString("yyyy-MM-dd") ?? "с начала учёта"} - {end?.ToString("yyyy-MM-dd") ?? "по конец учёта"}";
            string header = "Название материала\tСумма расхода\tЕдиница измерения";
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

            var lines = results.Select(row =>
                $"{row["name"]}\t{row["total_quantity"]}\t{row["unit"]}");

            string content = string.Join("\n", lines);

            if (!previewOnly)
            {
                SaveReport("consumption", start, end, $"{title}\n{period}\n\n{header}\n{content}", authorId);
            }

            return content;
        }

        public string GenerateAverageConsumptionReport(DateTime start, DateTime end, int authorId, bool previewOnly) // Конец метода GenerateAverageConsumptionReport
        {
            string title = "Отчёт по среднему расходу материалов";
            string period = $"{start:yyyy-MM-dd} - {end:yyyy-MM-dd}";
            string header = "Название материала\tСредний расход за день\tЕдиница измерения";

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

            // Формируем строки отчёта
            var lines = results.Select(row =>
                $"{row["name"]}\t{Math.Round(Convert.ToDouble(row["average_daily"]), 2)}\t{row["unit"]}");

            string content = string.Join("\n", lines);

            // Сохраняем отчёт, если не превью
            if (!previewOnly)
            {
                SaveReport("average_consumption", start, end, $"{title}\n{period}\n\n{header}\n{content}", authorId);
            }

            return content;
        } // Конец метода GenerateAverageConsumptionReport


        public string GenerateRemainingMaterialsReport(int authorId, bool previewOnly) // Конец метода GenerateRemainingMaterialsReport
        {
            string title = "Отчёт по остаткам материалов";
            string header = "Название материала\tСостояние";

            string query = "SELECT m.name, " +
                           "CASE " +
                           "    WHEN COALESCE(SUM(su.quantity), 0) - COALESCE(SUM(sm.quantity), 0) <= 0 THEN 'Израсходован' " +
                           "    ELSE 'Остаток: ' || (COALESCE(SUM(su.quantity), 0) - COALESCE(SUM(sm.quantity), 0)) " +
                           "END AS status " +
                           "FROM Materials m " +
                           "LEFT JOIN Supplies su ON su.material_id = m.id " +
                           "LEFT JOIN SpentMaterials sm ON sm.material_id = m.id " +
                           "GROUP BY m.name";

            var results = DatabaseHelper.ExecuteQuery(query);

            var lines = results.Select(row =>
                $"{row["name"]}\t{row["status"]}");

            string content = string.Join("\n", lines);

            if (!previewOnly)
            {
                SaveReport("remaining_materials", null, null, $"{title}\n\n{header}\n{content}", authorId);
            }

            return content;
        }

        public string GenerateSuppliesReport(int authorId, bool previewOnly) // Конец метода GenerateSuppliesReport
        {
            string title = "Отчёт по поставкам";
            string header = "№\tДата поставки\tНазвание поставщика\tНазвание материала\tКоличество\tЕдиница измерения";

            string query = "SELECT su.id, su.date, s.name AS supplier, m.name AS material, su.quantity, m.unit " +
                           "FROM Supplies su " +
                           "JOIN Suppliers s ON su.supplier_id = s.id " +
                           "JOIN Materials m ON su.material_id = m.id";

            var results = DatabaseHelper.ExecuteQuery(query);

            var lines = results.Select(row =>
                $"{row["id"]}\t{row["date"]}\t{row["supplier"]}\t{row["material"]}\t{row["quantity"]}\t{row["unit"]}");

            string content = string.Join("\n", lines);

            if (!previewOnly)
            {
                SaveReport("supplies", null, null, $"{title}\n\n{header}\n{content}", authorId);
            }

            return content;
        }

        private void SaveReport(string reportType, DateTime? start, DateTime? end, string content, int authorId) // Конец метода SaveReport
        {
            string sql = "INSERT INTO Reports (report_type, period_start, period_end, content) VALUES (@report_type, @period_start, @period_end, @content)";
            var parameters = new Dictionary<string, object>
            {
                { "@report_type", reportType },
                { "@period_start", start.HasValue ? start.Value : DBNull.Value },
                { "@period_end", end.HasValue ? end.Value : DBNull.Value },
                { "@content", content }
            };

            DatabaseHelper.ExecuteNonQuery(sql, parameters);
        }

        public List<Report> GetAllReports() // Конец метода GetAllReports
        {
            string sql = "SELECT * FROM Reports";
            var results = DatabaseHelper.ExecuteQuery(sql);

            return results.Select(row => new Report
            {
                id = Convert.ToInt32(row["id"]),
                report_type = row["report_type"]?.ToString(),
                period_start = row["period_start"] != DBNull.Value ? Convert.ToDateTime(row["period_start"]) : (DateTime?)null,
                period_end = row["period_end"] != DBNull.Value ? Convert.ToDateTime(row["period_end"]) : (DateTime?)null,
                content = row["content"]?.ToString()
            }).ToList();
        } // Конец метода GetAllReports

    } // Конец класса ReportService

    public class ReportController: IController
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public object Handle(HttpContext context, string? method) // Конец метода Handle
        {
            dynamic result;
            
            switch (method?.ToLower())
            {
                case "consumption":
                    var start = context.Request.Query.ContainsKey("start") ? DateTime.Parse(context.Request.Query["start"]) : (DateTime?)null;
                    var end = context.Request.Query.ContainsKey("end") ? DateTime.Parse(context.Request.Query["end"]) : (DateTime?)null;
                    var authorId = int.Parse(context.Request.Query["author_id"]);
                    var previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateConsumptionReport(start, end, authorId, previewOnly);
                    break;

                case "average_consumption":
                    var startMandatory = DateTime.Parse(context.Request.Query["start"]);
                    var endMandatory = DateTime.Parse(context.Request.Query["end"]);
                    authorId = int.Parse(context.Request.Query["author_id"]);
                    previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateAverageConsumptionReport(startMandatory, endMandatory, authorId, previewOnly);
                    break;

                case "remaining":
                    authorId = int.Parse(context.Request.Query["author_id"]);
                    previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateRemainingMaterialsReport(authorId, previewOnly);
                    break;

                case "supplies":
                    authorId = int.Parse(context.Request.Query["author_id"]);
                    previewOnly = context.Request.Query.ContainsKey("preview") && bool.Parse(context.Request.Query["preview"]);

                    result = _reportService.GenerateSuppliesReport(authorId, previewOnly);
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
        } // Конец метода Handle
    } // Конец класса ReportController

    public class Report
    {
        public int id;
        public string? report_type;
        public DateTime? period_start;
        public DateTime? period_end;
        public string? content;
    }
}