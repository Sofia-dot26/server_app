using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public interface ISpendMaterialService
    {
        bool AddSpend(int material_id, int quantity, DateTime date);
        bool UpdateSpend(int id, int material_id, int quantity, DateTime date);
        bool DeleteSpend(int id);
        Spend? GetSpend(int id);
        List<Spend> GetAllSpentMaterials();
    }
}
