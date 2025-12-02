using System;

namespace SistemaChamados.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }
        public string Response { get; set; }
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string AudioBase64 { get; set; }
    }
}