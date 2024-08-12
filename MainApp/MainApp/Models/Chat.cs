using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MainApp.Models
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }
        [Column("Master")]
        public string Username { get; set; }
        [Required]
        public string Source { get; set; }
        [Required]
        public string GPT { get; set; }
    }
}
