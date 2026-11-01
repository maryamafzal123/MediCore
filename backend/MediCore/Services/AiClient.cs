using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MediCore.Services
{
    // Talks to the Python AI service (FastAPI + LangGraph).
    // .NET owns users, auth and chat history; Python only "thinks".
    public class AiClient
    {
        private readonly HttpClient _http;

        public AiClient(HttpClient http) => _http = http;

        public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync("/chat", request, ct);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AiChatResponse>(cancellationToken: ct))!;
        }
    }

    public class AiChatRequest
    {
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("history")] public List<AiHistoryItem> History { get; set; } = new();
        [JsonPropertyName("user")] public AiUser User { get; set; } = new();
        // The user's own JWT: the AI calls the API *as this user*, so role rules still apply
        [JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
    }

    public class AiHistoryItem
    {
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
    }

    public class AiUser
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
    }

    public class AiChatResponse
    {
        [JsonPropertyName("reply")] public string Reply { get; set; } = string.Empty;
        [JsonPropertyName("tools_used")] public List<string> ToolsUsed { get; set; } = new();
    }
}
