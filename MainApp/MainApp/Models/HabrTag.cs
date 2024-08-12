using System.ComponentModel.DataAnnotations;

namespace MainApp.Models
{
    public class HabrTag
    {
        public HabrTag(string link, string name)
        {
            Link = link;
            Name = name;
        }

        [Key]
        public int Id { get; set; }
        [Required]
        public string Link { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
