using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WeatherApp.Core.Interfaces;
using WeatherApp.Core.Models;
using WeatherApp.Core.ViewModels;
using Microsoft.AspNetCore.Identity;
using WeatherApp.Data;
using WeatherApp.Services;

namespace WeatherApp.Web.Controllers
{
    [Authorize]
    public class WeatherController : Controller
    {
        private readonly IWeatherManager _weatherManager;
        private readonly IFavoriteCityService _favoriteCityService;
        private readonly ILogger<WeatherController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public WeatherController(IWeatherManager weatherManager, IFavoriteCityService favoriteCityService, ILogger<WeatherController> logger, UserManager<ApplicationUser> userManager)
        {
            _weatherManager = weatherManager;
            _favoriteCityService = favoriteCityService;
            _logger = logger;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var favoriteCities = await _favoriteCityService.GetFavoriteCitiesAsync(user.Id);
            var hottestCity = await _favoriteCityService.GetHottestCityAsync(user.Id);
            var coldestCity = await _favoriteCityService.GetColdestCityAsync(user.Id);

            var viewModel = new WeatherViewModel();

            viewModel.FavoriteCitiesWeather = new List<FavoriteCityWeather>();
            foreach (var city in favoriteCities)
            {
                var temp = await _weatherManager.GetAverageTemperatureAsync(city.CityName);
                viewModel.FavoriteCitiesWeather.Add(new FavoriteCityWeather
                {
                    Id = city.Id,
                    CityName = city.CityName,
                    AverageTemperature = temp
                });
            }

            viewModel.FavoriteCitiesSummary = new FavoriteCitiesSummary
            {
                HottestCity = hottestCity?.CityName,
                HottestTemperature = hottestCity != null ? await _weatherManager.GetAverageTemperatureAsync(hottestCity.CityName) : null,
                ColdestCity = coldestCity?.CityName,
                ColdestTemperature = coldestCity != null ? await _weatherManager.GetAverageTemperatureAsync(coldestCity.CityName) : null
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(WeatherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var favoriteCities = await _favoriteCityService.GetFavoriteCitiesAsync(user.Id);
                var hottestCity = await _favoriteCityService.GetHottestCityAsync(user.Id);
                var coldestCity = await _favoriteCityService.GetColdestCityAsync(user.Id);

                model.FavoriteCitiesWeather = new List<FavoriteCityWeather>();
                foreach (var city in favoriteCities)
                {
                    var temp = await _weatherManager.GetAverageTemperatureAsync(city.CityName);
                    model.FavoriteCitiesWeather.Add(new FavoriteCityWeather
                    {
                        Id = city.Id,
                        CityName = city.CityName,
                        AverageTemperature = temp
                    });
                }

                model.FavoriteCitiesSummary = new FavoriteCitiesSummary
                {
                    HottestCity = hottestCity?.CityName,
                    HottestTemperature = hottestCity != null ? await _weatherManager.GetAverageTemperatureAsync(hottestCity.CityName) : null,
                    ColdestCity = coldestCity?.CityName,
                    ColdestTemperature = coldestCity != null ? await _weatherManager.GetAverageTemperatureAsync(coldestCity.CityName) : null
                };

                return View(model);
            }


            var cityNames = model.CityNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(c => c.Trim())
                                           .Where(c => !string.IsNullOrEmpty(c))
                                           .ToList();

            foreach (var city in cityNames)
            {
                var averageTemp = await _weatherManager.GetAverageTemperatureAsync(city);
                model.CitiesWeather.Add(new CityWeather
                {
                    CityName = city,
                    AverageTemperature = averageTemp
                });
            }

            var userFavorites = await _favoriteCityService.GetFavoriteCitiesAsync((await _userManager.GetUserAsync(User)).Id);
            var hottestFavorite = await _favoriteCityService.GetHottestCityAsync((await _userManager.GetUserAsync(User)).Id);
            var coldestFavorite = await _favoriteCityService.GetColdestCityAsync((await _userManager.GetUserAsync(User)).Id);

            model.FavoriteCitiesWeather = new List<FavoriteCityWeather>();
            foreach (var city in userFavorites)
            {
                var temp = await _weatherManager.GetAverageTemperatureAsync(city.CityName);
                model.FavoriteCitiesWeather.Add(new FavoriteCityWeather
                {
                    Id = city.Id,
                    CityName = city.CityName,
                    AverageTemperature = temp
                });
            }

            model.FavoriteCitiesSummary = new FavoriteCitiesSummary
            {
                HottestCity = hottestFavorite?.CityName,
                HottestTemperature = hottestFavorite != null ? await _weatherManager.GetAverageTemperatureAsync(hottestFavorite.CityName) : null,
                ColdestCity = coldestFavorite?.CityName,
                ColdestTemperature = coldestFavorite != null ? await _weatherManager.GetAverageTemperatureAsync(coldestFavorite.CityName) : null
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddFavorite(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
            {
                TempData["Error"] = "Şehir adı boş olamaz.";
                return RedirectToAction("Index");
            }

            var userFavorites = await _favoriteCityService.GetFavoriteCitiesAsync((await _userManager.GetUserAsync(User)).Id);

            bool isAlreadyFavorite = userFavorites
             .Any(c => string.Equals(c.CityName, cityName, StringComparison.OrdinalIgnoreCase));

            if (isAlreadyFavorite)
            {
                TempData["Error"] = $"{cityName} zaten favorilerde mevcut.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            await _favoriteCityService.AddFavoriteCityAsync(user.Id, cityName);
            TempData["Success"] = $"{cityName} favori şehirlere eklendi.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFavorite(int favoriteCityId)
        {
            var user = await _userManager.GetUserAsync(User);
            await _favoriteCityService.RemoveFavoriteCityAsync(user.Id, favoriteCityId);
            TempData["Success"] = "Favori şehir silindi.";
            return RedirectToAction("Index");
        }
    }
}
