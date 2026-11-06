using System.ComponentModel.DataAnnotations;

namespace Olomove.Models;

public class AboutPage
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "Olomove taneční studio";

    [Required]
    public string Content { get; set; } = "Naše studio vzniklo z vášně pro tanec a touhy vytvořit komunitu lidí, kteří sdílejí radost z hudby a pohybu. " +
        "Nabízíme širokou škálu tanečních stylů – od moderního tance, přes street dance, až po ladné contemporary lekce. Každý si u nás najde to své, " +
        "bez ohledu na věk nebo zkušenosti.\r\n\r\nCo nás odlišuje? Především individuální přístup a přátelská atmosféra. Naši lektoři jsou nejen zkušení " +
        "profesionálové, ale také lidé, kteří vás dokážou motivovat a podpořit na vaší taneční cestě. Věříme, že tanec není jen o technice, ale především " +
        "o emocích, radosti a sebevyjádření.\r\n\r\nTěšíme se na vás na parketu!";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
