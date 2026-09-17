using System.Text.Json.Nodes;

namespace Sveda.Client;

public sealed class HistoriesResource
{
    private readonly SvedaHttp http;

    internal HistoriesResource(SvedaHttp http)
    {
        this.http = http;
    }

    public Task<JsonNode> ListAsync(CancellationToken cancellationToken = default)
    {
        return http.RequestJsonAsync(HttpMethod.Get, "/sveda/chat-histories", cancellationToken: cancellationToken);
    }

    public Task<JsonNode> GetAsync(string chatId, CancellationToken cancellationToken = default)
    {
        return http.RequestJsonAsync(HttpMethod.Get, HistoryPath(chatId), cancellationToken: cancellationToken);
    }

    public Task<JsonNode> RenameAsync(string chatId, string title, CancellationToken cancellationToken = default)
    {
        return http.RequestJsonAsync(HttpMethod.Patch, HistoryPath(chatId), new Dictionary<string, string> { ["title"] = title }, cancellationToken);
    }

    public Task<JsonNode> DeleteAsync(string chatId, CancellationToken cancellationToken = default)
    {
        return http.RequestJsonAsync(HttpMethod.Delete, HistoryPath(chatId), cancellationToken: cancellationToken);
    }

    private static string HistoryPath(string chatId)
    {
        return "/sveda/chat-histories/" + Uri.EscapeDataString(chatId);
    }
}
