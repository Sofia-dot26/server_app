using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    // Управление поставщиками
    public interface ISupplierService
    {
        bool AddSupplier(string name, string contactInfo);
        bool UpdateSupplier(int id, string name, string contactInfo);
        bool DeleteSupplier(int id);

        Supplier? GetSupplier(int id);
        List<Supplier> GetAllSuppliers();
    }
}
