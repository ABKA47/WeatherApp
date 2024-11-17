using System;
using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Core.Models
{
    public class WeatherCache
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public double Temperature { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }
    }
}
