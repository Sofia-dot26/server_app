using AccountingServer.Models;
using ServerApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
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
}
