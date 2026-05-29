using System;

namespace Grex.Models
{
    public sealed class AiChatMessage
    {
        public string Role { get; set; } = "assistant";
        public string Speaker { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string TimestampText => Timestamp.ToString("HH:mm:ss");
    }
}
