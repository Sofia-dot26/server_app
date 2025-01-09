using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
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
}
