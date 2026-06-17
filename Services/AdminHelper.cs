using System;
using System.IO;
using System.Security.Principal;

namespace Grex.Services
{
    /// <summary>
    /// Helper class for detecting administrator privileges.
    /// </summary>
    public static class AdminHelper
    {
        /// <summary>
        /// Checks if the current process is running with administrator privileges.
        /// </summary>
        public static bool IsRunAsAdmin()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void LogToFile(string message) => LogService.Write(message);
    }
}

