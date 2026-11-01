using System.ComponentModel.DataAnnotations;

namespace MediCore.DTOs
{
    public class SendMessageDto
    {
        [Required, StringLength(2000, MinimumLength = 1)]
        public string Message { get; set; } = string.Empty;
    }

    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ChatReplyDto
    {
        public int ConversationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public ChatMessageDto UserMessage { get; set; } = new();
        public ChatMessageDto AssistantMessage { get; set; } = new();
        public List<string> ToolsUsed { get; set; } = new();
    }
}
