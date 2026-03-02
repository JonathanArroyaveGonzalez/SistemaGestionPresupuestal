using Microsoft.AspNetCore.Authorization;

namespace SAPFIAI.Infrastructure.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var permissionClaim = context.User.FindFirst(c => c.Type == "permission" && c.Value == requirement.Permission);

        if (permissionClaim != null)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
