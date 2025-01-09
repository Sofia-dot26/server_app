using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    // Управление материалами
    public interface IMaterialService
    {
        bool AddMaterial(string name,  string unit);
        bool UpdateMaterial(int id, string name, string unit);
        bool DeleteMaterial(int id);
        Material? GetMaterial(int id);
        List<Material> GetAllMaterials();
    }
}
