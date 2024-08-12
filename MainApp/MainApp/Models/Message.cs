using System.ComponentModel.DataAnnotations;

namespace MainApp.Models
{

    public class Message
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        public bool By { get; set; }
        [Required]
        public DateTime Time { get; set; }
        [Required]
        public int ChatId { get; set; }
    }
}
