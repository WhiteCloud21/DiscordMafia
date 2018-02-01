using DiscordMafia.Roles;
using System.Collections.Generic;
using System.Linq;

namespace MafiaWeb.ViewModels.Role
{
    public class RoleInfo
    {
        private static IEnumerable<RoleInfo> availableRoles;
        public BaseRole Role;

        public static IEnumerable<RoleInfo> AvailableRoles
        {
            get
            {
                if (availableRoles == null)
                {
                    availableRoles = from r in DiscordMafia.Config.Roles.GetAllRoles() select new RoleInfo() { Role = r };
                }
                return availableRoles;
            }
        }
    }
}
