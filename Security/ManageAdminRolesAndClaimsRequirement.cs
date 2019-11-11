using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement.Security
{
    // This is our requirement which needed as a generic parameter in Handler
    public class ManageAdminRolesAndClaimsRequirement : IAuthorizationRequirement
    {
    }
}
