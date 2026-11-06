using System.ComponentModel.DataAnnotations;

namespace Olomove.Models;

public class AboutPage
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "Olomove taneční studio";

    [Required]
    public string Content { get; set; } = "Jsme taneční studio";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
