namespace MediCore.Models
{
    // One chat thread. "New chat" = new Conversation row.
    public class Conversation : BaseEntity
    {
        public string Title { get; set; } = "New chat";

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    // One message inside a conversation. Role = "user" or "assistant".
    public class ChatMessage : BaseEntity
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
    }
}
