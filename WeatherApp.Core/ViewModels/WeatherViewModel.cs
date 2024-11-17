using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WeatherApp.Core.Models;

namespace WeatherApp.Core.ViewModels
{
    public class WeatherViewModel
    {
        [Required(ErrorMessage = "Lütfen en az bir şehir adı giriniz.")]
        [Display(Name = "Şehir Adları")]
        public string CityNames { get; set; } 

        public List<CityWeather> CitiesWeather { get; set; } = new List<CityWeather>();

        public FavoriteCitiesSummary? FavoriteCitiesSummary { get; set; }

        public List<FavoriteCityWeather> FavoriteCitiesWeather { get; set; } = new List<FavoriteCityWeather>();
    }

}
