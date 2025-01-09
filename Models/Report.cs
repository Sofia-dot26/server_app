using AccountingServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AccountingServer.Models
{
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
