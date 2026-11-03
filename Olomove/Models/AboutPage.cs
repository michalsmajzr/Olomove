namespace Olomove.Models;

public class AboutPage
{
	public int Id { get; set; }
	public string Title { get; set; } = "Olomove taneční studio";
	public string Content { get; set; } = "Jsme taneční studio";
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}