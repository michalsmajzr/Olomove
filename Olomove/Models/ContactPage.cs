using System.ComponentModel.DataAnnotations;

namespace Olomove.Models;

public class ContactPage
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = "Paní Morávková";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "jarmila.moravkova@gmail.com";

    [Required, MaxLength(40)]
    public string Phone { get; set; } = "+420 653 646 545";

    [Required, MaxLength(120)]
    public string City { get; set; } = "Olomouc";

    [Required, MaxLength(200)]
    public string Address { get; set; } = "Sokolská 550/22";

    [Required, MaxLength(20)]
    public string PostCode { get; set; } = "779 00";

    [Required]
    public string MapUrl { get; set; } = "https://frame.mapy.cz/";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
