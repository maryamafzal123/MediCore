using MediCore.Data;
using MediCore.DTOs;
using MediCore.Models;
using MediCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MediCore.Controllers
{
    // Chat memory lives HERE (PostgreSQL). Every user only sees their own conversations.
    // Flow: Angular → .NET (save + load history) → Python AI → .NET (save reply) → Angular
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private const int HistoryLimit = 20;   // last N messages sent to the LLM as context

        private readonly AppDbContext _context;
        private readonly AiClient _ai;
        private readonly ILogger<ChatController> _logger;

        public ChatController(AppDbContext context, AiClient ai, ILogger<ChatController> logger)
        {
            _context = context;
            _ai = ai;
            _logger = logger;
        }

        // GET api/chat/conversations — sidebar list
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = User.GetUserId();
            var list = await _context.Conversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new ConversationDto { Id = c.Id, Title = c.Title, UpdatedAt = c.UpdatedAt })
                .ToListAsync();
            return Ok(list);
        }

        // POST api/chat/conversations — "New chat"
        [HttpPost("conversations")]
        public async Task<IActionResult> CreateConversation()
        {
            var conversation = new Conversation { UserId = User.GetUserId() };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            return Ok(new ConversationDto { Id = conversation.Id, Title = conversation.Title, UpdatedAt = conversation.UpdatedAt });
        }

        // GET api/chat/conversations/5/messages
        [HttpGet("conversations/{id:int}/messages")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var conversation = await FindOwned(id);
            if (conversation == null) return NotFound("Conversation not found");

            var messages = await _context.ChatMessages
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
                .Select(m => ToDto(m))
                .ToListAsync();
            return Ok(messages);
        }

        // POST api/chat/conversations/5/messages — send a message, get the AI reply
        [HttpPost("conversations/{id:int}/messages")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendMessageDto dto, CancellationToken ct)
        {
            var conversation = await FindOwned(id);
            if (conversation == null) return NotFound("Conversation not found");

            // 1) Load recent history (this is the bot's "memory")
            var history = await _context.ChatMessages
                .Where(m => m.ConversationId == id)
                .OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
                .Take(HistoryLimit)
                .Select(m => new AiHistoryItem { Role = m.Role, Content = m.Content })
                .ToListAsync(ct);
            history.Reverse();

            // 2) Save the user's message
            var userMessage = new ChatMessage { ConversationId = id, Role = "user", Content = dto.Message.Trim() };
            _context.ChatMessages.Add(userMessage);

            if (conversation.Title == "New chat")
                conversation.Title = dto.Message.Length > 40 ? dto.Message[..40].Trim() + "…" : dto.Message.Trim();

            // 3) Ask the AI service, forwarding this user's JWT
            var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            AiChatResponse ai;
            try
            {
                ai = await _ai.ChatAsync(new AiChatRequest
                {
                    Message = userMessage.Content,
                    History = history,
                    Token = token,
                    User = new AiUser
                    {
                        Name = User.FindFirst("FullName")?.Value ?? "",
                        Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? ""
                    }
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI service call failed");
                ai = new AiChatResponse
                {
                    Reply = "Sorry, the assistant is not available right now. Please make sure the AI service is running and try again."
                };
            }

            // 4) Save the assistant's reply
            var assistantMessage = new ChatMessage { ConversationId = id, Role = "assistant", Content = ai.Reply };
            _context.ChatMessages.Add(assistantMessage);
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(CancellationToken.None);

            return Ok(new ChatReplyDto
            {
                ConversationId = id,
                Title = conversation.Title,
                UserMessage = ToDto(userMessage),
                AssistantMessage = ToDto(assistantMessage),
                ToolsUsed = ai.ToolsUsed
            });
        }

        // PUT api/chat/conversations/5 — rename
        [HttpPut("conversations/{id:int}")]
        public async Task<IActionResult> Rename(int id, [FromBody] ConversationDto dto)
        {
            var conversation = await FindOwned(id);
            if (conversation == null) return NotFound("Conversation not found");
            conversation.Title = string.IsNullOrWhiteSpace(dto.Title) ? conversation.Title : dto.Title.Trim();
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE api/chat/conversations/5
        [HttpDelete("conversations/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var conversation = await FindOwned(id);
            if (conversation == null) return NotFound("Conversation not found");
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
            return Ok();
        }

        private Task<Conversation?> FindOwned(int id)
        {
            var userId = User.GetUserId();
            return _context.Conversations.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        }

        private static ChatMessageDto ToDto(ChatMessage m) =>
            new() { Id = m.Id, Role = m.Role, Content = m.Content, CreatedAt = m.CreatedAt };
    }
}
