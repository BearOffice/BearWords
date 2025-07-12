using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BearWordsAPI;

public class ClientValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var userName = httpContext.User.Identity!.Name!;
        var db = httpContext.RequestServices.GetRequiredService<BearWordsContext>();

        var routeValues = httpContext.Request.RouteValues;

        if (routeValues.TryGetValue("clientId", out var clientIdObj) && clientIdObj is string clientId)
        {
            var clientExists = await db.Syncs.WhereUser(userName).AnyAsync(s => s.ClientId == clientId);
            if (!clientExists)
            {
                return TypedResults.NotFound();
            }
        }

        return await next(context);
    }
}
