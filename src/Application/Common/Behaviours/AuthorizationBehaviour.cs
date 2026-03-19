using System.Reflection;
using SAPFIAI.Application.Common.Exceptions;
using SAPFIAI.Application.Common.Interfaces;
using SAPFIAI.Application.Common.Security;

namespace SAPFIAI.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IUser _user;

    public AuthorizationBehaviour(IUser user)
    {
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();

        if (authorizeAttributes.Any())
        {
            if (_user.Id == null)
            {
                throw new UnauthorizedAccessException();
            }

            if (authorizeAttributes.Any(a => !string.IsNullOrWhiteSpace(a.Roles) || !string.IsNullOrWhiteSpace(a.Policy)))
            {
                throw new InvalidOperationException("Role and policy based authorization is no longer supported. Use endpoint-level Permit metadata instead.");
            }
        }

        return await next();
    }
}
