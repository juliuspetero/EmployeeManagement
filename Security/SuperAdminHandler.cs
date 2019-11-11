using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement.Security
{
    public class SuperAdminHandler :
        AuthorizationHandler<ManageAdminRolesAndClaimsRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageAdminRolesAndClaimsRequirement requirement)
        { 
            if (context.User.IsInRole("Super Admin"))
            {
                // Specified that the requirement has been successfully evaluated when a logged in user is in Super Admin role
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
