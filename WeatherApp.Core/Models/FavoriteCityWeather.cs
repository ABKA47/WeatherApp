using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherApp.Core.Models
{    public class FavoriteCityWeather
    {
        public int Id { get; set; }
        public string CityName { get; set; }
        public double? AverageTemperature { get; set; }
    }
}
