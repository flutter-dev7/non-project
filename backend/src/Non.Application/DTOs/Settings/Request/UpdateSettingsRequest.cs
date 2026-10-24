using System.ComponentModel.DataAnnotations;

namespace Non.Application.DTOs.Settings.Request;

public class UpdateSettingsRequest
{
    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Время должно быть в формате HH:mm, например 18:00.")]
    public string OrderCutoffTime { get; set; } = string.Empty;
}