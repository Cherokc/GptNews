using System.ComponentModel.DataAnnotations;

namespace MainApp.Models
{
    public class HabrNews
    {
        public HabrNews()
        {
        }

        public HabrNews(string link, string id, DateTime time, string timeToRead, string title, string description, string content)
        {
            Link = link;
            Id = id;
            Time = time;
            TimeToRead = timeToRead;
            Title = title;
            Description = description;
            Content = content;
        }

        [Key]
        public string Link { get; set; }
        [Required]
        public string Id { get; set; }
        [Required]
        public DateTime Time { get; set; }
        [Required]
        public string TimeToRead { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public string? Content { get; set; }
    }
}
