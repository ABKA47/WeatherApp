using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Core.Interfaces;
using WeatherApp.Core.Models;
using WeatherApp.Data;

namespace WeatherApp.Services
{
    public class FavoriteCityService : IFavoriteCityService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWeatherManager _weatherManager;

        public FavoriteCityService(ApplicationDbContext dbContext, IWeatherManager weatherManager)
        {
            _dbContext = dbContext;
            _weatherManager = weatherManager;
        }

        public async Task<IEnumerable<FavoriteCity>> GetFavoriteCitiesAsync(string userId)
        {
            return await _dbContext.FavoriteCities
                                   .Where(fc => fc.UserId == userId)
                                   .ToListAsync();
        }

        public async Task AddFavoriteCityAsync(string userId, string cityName)
        {
            var favoriteCity = new FavoriteCity
            {
                UserId = userId,
                CityName = cityName
            };

            _dbContext.FavoriteCities.Add(favoriteCity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveFavoriteCityAsync(string userId, int favoriteCityId)
        {
            var favoriteCity = await _dbContext.FavoriteCities
                                              .FirstOrDefaultAsync(fc => fc.Id == favoriteCityId && fc.UserId == userId);

            if (favoriteCity != null)
            {
                _dbContext.FavoriteCities.Remove(favoriteCity);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<FavoriteCity> GetHottestCityAsync(string userId)
        {
            var cities = await GetFavoriteCitiesAsync(userId);
            FavoriteCity hottestCity = null;
            double maxTemp = double.MinValue;

            foreach (var city in cities)
            {
                var temp = await _weatherManager.GetAverageTemperatureAsync(city.CityName);
                if (temp.HasValue && temp.Value > maxTemp)
                {
                    maxTemp = temp.Value;
                    hottestCity = city;
                }
            }

            return hottestCity;
        }

        public async Task<FavoriteCity> GetColdestCityAsync(string userId)
        {
            var cities = await GetFavoriteCitiesAsync(userId);
            FavoriteCity coldestCity = null;
            double minTemp = double.MaxValue;

            foreach (var city in cities)
            {
                var temp = await _weatherManager.GetAverageTemperatureAsync(city.CityName);
                if (temp.HasValue && temp.Value < minTemp)
                {
                    minTemp = temp.Value;
                    coldestCity = city;
                }
            }

            return coldestCity;
        }
    }
}
