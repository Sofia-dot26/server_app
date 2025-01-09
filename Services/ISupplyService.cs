using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public interface ISupplyService
    {
        bool AddSupply(int material_id, int supplier_id, int quantity, DateTime date);
        bool UpdateSupply(int id, int material_id, int supplier_id, int quantity, DateTime date);
        bool DeleteSupply(int id);
        Supply? GetSupply(int id);
        List<Supply> GetAllSupplies();
    }
}
