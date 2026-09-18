using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Sveda.Host;

public static class HostMcpEndpointExtensions
{
    public static IEndpointRouteBuilder MapSvedaMcp(this IEndpointRouteBuilder endpoints, HostManager host, string pattern = "/mcp/sveda")
    {
        endpoints.MapPost(pattern, async (HttpContext context, CancellationToken cancellationToken) =>
        {
            var bearer = HostManager.ReadBearerToken(context.Request.Headers.Authorization);
            var auth = await host.AuthenticateBearerTokenAsync(bearer).ConfigureAwait(false);
            if (auth is null)
            {
                return Results.Unauthorized();
            }

            if (!host.Authorize(auth.User))
            {
                return Results.Forbid();
            }

            await host.AfterAuthenticateAsync(auth.User).ConfigureAwait(false);

            JsonObject payload;
            try
            {
                payload = await JsonNode.ParseAsync(context.Request.Body, cancellationToken: cancellationToken).ConfigureAwait(false) as JsonObject
                    ?? new JsonObject();
            }
            catch
            {
                return Results.BadRequest();
            }

            var callContext = new HostCallContext
            {
                User = auth.User,
                PageContext = ReadPageContext(context.Request.Headers),
                ChatId = context.Request.Headers["X-Sveda-Chat-Id"].FirstOrDefault(),
            };

            var outcome = await HostMcpHandler.HandleAsync(host, payload, callContext, cancellationToken).ConfigureAwait(false);
            foreach (var (key, value) in outcome.Headers)
            {
                context.Response.Headers[key] = value;
            }

            if (outcome.Body is null)
            {
                return Results.StatusCode(outcome.StatusCode);
            }

            return Results.Json(outcome.Body, statusCode: outcome.StatusCode);
        });

        return endpoints;
    }

    private static IReadOnlyDictionary<string, object?>? ReadPageContext(IHeaderDictionary headers)
    {
        var raw = headers["X-Sveda-Page-Context"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 65536)
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(raw)?.AsObject().ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
        }
        catch
        {
            return null;
        }
    }
}
