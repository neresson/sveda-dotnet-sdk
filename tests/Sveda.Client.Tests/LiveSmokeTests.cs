using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Sveda.Client.Tests;

[Trait("Category", "Live")]
public sealed class LiveSmokeTests
{
    [Fact]
    public async Task RunsHealthMessageStreamAndHistoryFlow()
    {
        var baseUrl = Environment.GetEnvironmentVariable("SVEDA_BASE_URL")?.TrimEnd('/');
        var hostKey = Environment.GetEnvironmentVariable("SVEDA_HOST_KEY");
        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(hostKey))
        {
            return;
        }

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl + "/") };
        foreach (var path in new[] { "sveda/health", "sveda/ready" })
        {
            var payload = await http.GetFromJsonAsync<JsonElement>(path);
            Assert.True(payload.GetProperty("ok").GetBoolean());
        }

        var host = new SvedaClient(new SvedaClientOptions { BaseUrl = baseUrl, HostApiKey = hostKey });
        var token = await host.Embed.CreateTokenAsync(new CreateTokenRequest { VisitorId = "sdk-compat-dotnet" });
        Assert.StartsWith("sveda_embed_", token.Token);

        var embed = new SvedaClient(new SvedaClientOptions { BaseUrl = baseUrl, EmbedToken = token.Token });
        var eventTypes = new List<string>();
        await foreach (var eventItem in embed.Chat.CreateStreamedAsync(new
        {
            prompt = "compat stream",
            chatId = "sdk-compat-dotnet",
            messages = new[] { new { id = "m1", role = "user", content = "compat stream" } },
        }))
        {
            eventTypes.Add(eventItem.Type);
        }
        Assert.NotEmpty(eventTypes);

        var message = await embed.Chat.CreateAsync(new
        {
            prompt = "compat smoke",
            chatId = "sdk-compat-dotnet-json",
            messages = new[] { new { id = "m2", role = "user", content = "compat smoke" } },
        });
        Assert.False(string.IsNullOrWhiteSpace(message.Explanation));

        var histories = await embed.Histories.ListAsync();
        Assert.True(histories.ContainsKey("histories"));
    }
}
