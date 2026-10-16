using System.ComponentModel.DataAnnotations;

namespace VacationManager.Controllers;

public class UserForm
{
    public int Id { get; set; }

    [Required, StringLength(50, MinimumLength = 3)]
    [Display(Name = "Потребителско име")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Парола")]
    public string? Password { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Собствено име")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Роля")]
    public int RoleId { get; set; }

    [Display(Name = "Екип")]
    public int? TeamId { get; set; }
}
