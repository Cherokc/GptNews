using System.ComponentModel.DataAnnotations;

namespace MainApp.Models
{
    public class User
    {
        [Key]
        [Required(ErrorMessage = "Обязательно поле.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Имя пользователя должно содержать от 3 до 50 символов.")]

        public string Username { get; set; }

        [Required(ErrorMessage = "Обязательное поле.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Пароль должен содержать от 3 до 50 символов.")]

        public string Password { get; set; }
    }
}
