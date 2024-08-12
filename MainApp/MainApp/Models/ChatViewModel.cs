using System.ComponentModel.DataAnnotations;

namespace MainApp.Models
{
    public class ChatViewModel
    {
        public List<Message> Messages { get; set; }
        public int ChatId { get; set; }
        public string NewMessageText { get; set; }
        public List<Chat> Chats { get; set; }
        public List<DateTime>? LastTimeMessage { get; set; }
        public bool Success { get; set; }
    }
}