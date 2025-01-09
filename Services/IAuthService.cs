using AccountingServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingServer.Services
{
    public interface IAuthService
    {
        Session? Login(string username, string password, out User? user, out int? error, string ip = "127.0.0.1");
        bool Logout(int sessionId);
        Session? CreateSession(int userId, string ip);
        bool ValidateSession(int sessionId);
        bool RemoveSession(int sessionId);
        bool RemoveExpiredSessions();
        Session? GetSession(int sessionId);
        Session? GetSessionByUserId(int userId);
    }
}
