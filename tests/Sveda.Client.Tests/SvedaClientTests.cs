using Xunit;

namespace Sveda.Client.Tests;

public sealed class SvedaClientTests
{
    [Fact]
    public async Task ItIssuesEmbedTokensWithHostCredentials()
    {
        var handler = new StubHandler
        {
            JsonBody = """{"token":"sveda_embed_test","visitor_id":"visitor-1","expires_in":3600}""",
        };

        using var client = new SvedaClient(new SvedaClientOptions
        {
            BaseUrl = "https://sveda.test",
            HostApiKey = "host-secret",
            HttpMessageHandler = handler,
        });

        var response = await client.Embed.CreateTokenAsync(new CreateTokenRequest
        {
            VisitorId = "visitor-1",
            HostMcpUrl = "https://app.test/mcp/sveda",
            HostMcpToken = "mcp-token",
        });

        Assert.Equal("sveda_embed_test", response.Token);
        Assert.Equal("visitor-1", response.VisitorId);
        Assert.Equal(3600, response.ExpiresIn);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal("POST", recorded.Method);
        Assert.Equal("/sveda/embed/token", recorded.Uri);
        Assert.Equal("Bearer host-secret", recorded.Authorization);
        Assert.Contains("\"visitor_id\":\"visitor-1\"", recorded.Body);
        Assert.Contains("\"host_mcp_url\":\"https://app.test/mcp/sveda\"", recorded.Body);
        Assert.Contains("\"host_mcp_token\":\"mcp-token\"", recorded.Body);
    }

    [Fact]
    public async Task ItStreamsChatEvents()
    {
        var handler = new StubHandler
        {
            StreamBody = "data: {\"type\":\"message.start\"}\n\ndata: {\"type\":\"text.delta\",\"delta\":\"Hi\"}\n\ndata: [DONE]\n\n",
        };

        using var client = new SvedaClient(new SvedaClientOptions
        {
            BaseUrl = "https://sveda.test",
            EmbedToken = "embed-token",
            HttpMessageHandler = handler,
        });

        var events = new List<StreamEvent>();
        await foreach (var ev in client.Chat.CreateStreamedAsync(new
        {
            messages = new[] { new { role = "user", content = "Hello" } },
            chatId = "chat-1",
        }))
        {
            events.Add(ev);
        }

        Assert.Equal(2, events.Count);
        Assert.Equal("message.start", events[0].Type);
        Assert.Equal("text.delta", events[1].Type);
        Assert.Equal("Hi", events[1]["delta"]);

        var recorded = Assert.Single(handler.Requests);
        Assert.Equal("POST", recorded.Method);
        Assert.Equal("/sveda/stream", recorded.Uri);
        Assert.Equal("embed-token", recorded.EmbedToken);
        Assert.Contains("\"chatId\":\"chat-1\"", recorded.Body);
    }

    [Fact]
    public async Task ItFetchesMessageAndHistories()
    {
        var handler = new StubHandler
        {
            Responder = (request, _) =>
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                if (request.Method == HttpMethod.Post && path == "/sveda/message")
                {
                    return Json(new { explanation = "Hello", tokens_used = 12, chat_id = "chat-1" });
                }

                if (request.Method == HttpMethod.Get && path == "/sveda/chat-histories")
                {
                    return Json(new { histories = Array.Empty<object>() });
                }

                return Json(new { });
            },
        };

        using var client = new SvedaClient(new SvedaClientOptions
        {
            BaseUrl = "https://sveda.test",
            EmbedToken = "embed-token",
            HttpMessageHandler = handler,
        });

        var message = await client.Chat.CreateAsync(new
        {
            messages = new[] { new { role = "user", content = "Hello" } },
            chatId = "chat-1",
        });

        Assert.Equal("Hello", message.Explanation);
        Assert.Equal(12, message.TokensUsed);
        Assert.Equal("chat-1", message.ChatId);

        var histories = await client.Histories.ListAsync();
        Assert.NotNull(histories["histories"]);
    }

    [Fact]
    public async Task StartHostSessionAsyncReturnsOriginTokenExpiresAndAppearance()
    {
        var handler = new StubHandler
        {
            JsonBody = """{"token":"sveda_embed_host","visitor_id":"aspnet-playground","expires_in":1800,"appearance":{"accent":"#c45c26"}}""",
        };

        using var client = new SvedaClient(new SvedaClientOptions
        {
            BaseUrl = "https://sveda.test/",
            HostApiKey = "host-secret",
            HttpMessageHandler = handler,
        });

        var session = await client.StartHostSessionAsync(new CreateTokenRequest
        {
            VisitorId = "aspnet-playground",
        });

        Assert.Equal("https://sveda.test", session.Origin);
        Assert.Equal("sveda_embed_host", session.Token);
        Assert.Equal(1800, session.ExpiresIn);
        Assert.Equal("#c45c26", session.Appearance?["accent"]?.GetValue<string>());
        Assert.Equal("Bearer host-secret", Assert.Single(handler.Requests).Authorization);
    }

    [Fact]
    public async Task StartHostSessionAsyncRequiresBaseUrlAndHostApiKey()
    {
        using var client = new SvedaClient(new SvedaClientOptions
        {
            BaseUrl = "",
            HostApiKey = "",
            HttpMessageHandler = new StubHandler(),
        });

        var exception = await Assert.ThrowsAsync<SvedaConfigurationException>(() => client.StartHostSessionAsync(new CreateTokenRequest
        {
            VisitorId = "aspnet-playground",
        }));

        Assert.Contains("SVEDA_CLIENT_BASE_URL", exception.Message);
        Assert.Contains("SVEDA_CLIENT_HOST_API_KEY", exception.Message);
    }

    private static HttpResponseMessage Json(object payload)
    {
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
