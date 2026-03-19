using Microsoft.AspNetCore.Builder;

namespace SAPFIAI.Web.Infrastructure;

public static class PermitEndpointConventionBuilderExtensions
{
    public static TBuilder RequirePermit<TBuilder>(this TBuilder builder, string resource, string action)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.Add(endpointBuilder => endpointBuilder.Metadata.Add(new PermitRequirementMetadata(resource, action)));
        return builder;
    }
}