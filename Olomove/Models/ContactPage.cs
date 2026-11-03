namespace Olomove.Models
{
	public class ContactPage
	{
		public int Id { get; set; }

		// kontaktni osoba (jenom jedna)
		public string Name { get; set; } = "Paní Morávková";
		public string Email { get; set; } = "jarmila.moravkova@gmail.com";
		public string Phone { get; set; } = "+420 653 646 545";

		// adresa
		public string City { get; set; } = "Olomouc";
		public string Address { get; set; } = "Sokolská 550/22";
		public string PostCode { get; set; } = "779 00";

		// url umbed pro mapu
		public string MapUrl { get; set; } = "https://frame.mapy.cz/";

		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}