using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherApp.Core.Models
{
    public class FavoriteCity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CityName { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
    }
}
