using System.ComponentModel.DataAnnotations;

namespace Web.ViewModels;

public class NewsIndexViewModel
{
    [Required(ErrorMessage = "Заглавието е задължително.")]
    [StringLength(200, ErrorMessage = "Заглавието не може да бъде по-дълго от 200 символа.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Текстът е задължителен.")]
    [StringLength(4000, ErrorMessage = "Текстът не може да бъде по-дълъг от 4000 символа.")]
    public string Text { get; set; } = string.Empty;

    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public List<NewsItemViewModel> News { get; set; } = new List<NewsItemViewModel>();
}

public class NewsItemViewModel
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string PreviewText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedAtLabel { get; set; } = string.Empty;
}
