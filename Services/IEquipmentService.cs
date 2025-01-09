using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public interface IEquipmentService
    {
        bool AddEquipment(string name, string description);
        bool UpdateEquipment(int id, string name, string description);
        bool DeleteEquipment(int id);
        Equipment? GetEquipment(int id);
        List<Equipment> GetAllEquipment();
    }
}
