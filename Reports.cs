using Microsoft.AspNetCore.Http;
using ServerApp;

namespace Reports
{
    public interface IReportService
    {
        string GenerateConsumptionReport(DateTime? start, DateTime? end, int authorId, bool previewOnly); // ����� ������ GenerateConsumptionReport
        string GenerateAverageConsumptionReport(DateTime start, DateTime end, int authorId, bool previewOnly); // ����� ������ GenerateAverageConsumptionReport
        string GenerateRemainingMaterialsReport(int authorId, bool previewOnly); // ����� ������ GenerateRemainingMaterialsReport
        string GenerateSuppliesReport(int authorId, bool previewOnly); // ����� ������ GenerateSuppliesReport
        List<Report> GetAllReports(); // ����� ������ GetAllReports
    }

    public class ReportService : IReportService
    {
        public string GenerateConsumptionReport(DateTime? start, DateTime? end, int authorId, bool previewOnly) // ����� ������ GenerateConsumptionReport
        {
            string title = "����� �� ������� ����������";
            string period = $"{start?.ToString("yyyy-MM-dd") ?? "� ������ �����"} - {end?.ToString("yyyy-MM-dd") ?? "�� ����� �����"}";
            string header = "�������� ���������\t����� �������\t������� ���������";
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

        public string GenerateAverageConsumptionReport(DateTime start, DateTime end, int authorId, bool previewOnly) // ����� ������ GenerateAverageConsumptionReport
        {
            string title = "����� �� �������� ������� ����������";
            string period = $"{start:yyyy-MM-dd} - {end:yyyy-MM-dd}";
            string header = "�������� ���������\t������� ������ �� ����\t������� ���������";

            // ���������� ���� � �������
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

            // ��������� ������ ������
            var lines = results.Select(row =>
                $"{row["name"]}\t{Math.Round(Convert.ToDouble(row["average_daily"]), 2)}\t{row["unit"]}");

            string content = string.Join("\n", lines);

            // ��������� �����, ���� �� ������
            if (!previewOnly)
            {
                SaveReport("average_consumption", start, end, $"{title}\n{period}\n\n{header}\n{content}", authorId);
            }

            return content;
        } // ����� ������ GenerateAverageConsumptionReport


        public string GenerateRemainingMaterialsReport(int authorId, bool previewOnly) // ����� ������ GenerateRemainingMaterialsReport
        {
            string title = "����� �� �������� ����������";
            string header = "�������� ���������\t���������";

            string query = "SELECT m.name, " +
                           "CASE " +
                           "    WHEN COALESCE(SUM(su.quantity), 0) - COALESCE(SUM(sm.quantity), 0) <= 0 THEN '������������' " +
                           "    ELSE '�������: ' || (COALESCE(SUM(su.quantity), 0) - COALESCE(SUM(sm.quantity), 0)) " +
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

        public string GenerateSuppliesReport(int authorId, bool previewOnly) // ����� ������ GenerateSuppliesReport
        {
            string title = "����� �� ���������";
            string header = "�\t���� ��������\t�������� ����������\t�������� ���������\t����������\t������� ���������";

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

        private void SaveReport(string reportType, DateTime? start, DateTime? end, string content, int authorId) // ����� ������ SaveReport
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

        public List<Report> GetAllReports() // ����� ������ GetAllReports
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
        } // ����� ������ GetAllReports

    } // ����� ������ ReportService

    public class ReportController: IController
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        public object Handle(HttpContext context, string? method) // ����� ������ Handle
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
                    result = new { message = "����� �� ������." };
                    break;
            }

            return result;
        } // ����� ������ Handle
    } // ����� ������ ReportController

    public class Report
    {
        public int id;
        public string? report_type;
        public DateTime? period_start;
        public DateTime? period_end;
        public string? content;
    }
}