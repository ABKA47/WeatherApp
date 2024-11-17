using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using WeatherApp.Core.Interfaces;
using WeatherApp.Core.Models;
using WeatherApp.Services;
using WeatherApp.Data;
using Xunit;

namespace WeatherApp.Tests
{
    public class FavoriteCityServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IWeatherManager> _weatherManagerMock;
        private readonly IFavoriteCityService _service;

        public FavoriteCityServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _weatherManagerMock = new Mock<IWeatherManager>();
            _service = new FavoriteCityService(_dbContext, _weatherManagerMock.Object);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task AddFavoriteCityAsync_AddsCityToDatabase()
        {
            var userId = "user1";
            var cityName = "Istanbul";

            await _service.AddFavoriteCityAsync(userId, cityName);

            var city = await _dbContext.FavoriteCities.FirstOrDefaultAsync(fc => fc.UserId == userId && fc.CityName == cityName);
            Assert.NotNull(city);
            Assert.Equal(userId, city.UserId);
            Assert.Equal(cityName, city.CityName);
        }

        [Fact]
        public async Task RemoveFavoriteCityAsync_RemovesCityFromDatabase()
        {

            var userId = "user1";
            var cityName = "Ankara";

            _dbContext.FavoriteCities.Add(new FavoriteCity { UserId = userId, CityName = cityName });
            await _dbContext.SaveChangesAsync();

            var city = await _dbContext.FavoriteCities.FirstOrDefaultAsync(fc => fc.UserId == userId && fc.CityName == cityName);
            Assert.NotNull(city);


            await _service.RemoveFavoriteCityAsync(userId, city.Id);


            var removedCity = await _dbContext.FavoriteCities.FirstOrDefaultAsync(fc => fc.Id == city.Id);
            Assert.Null(removedCity);
        }

        [Fact]
        public async Task GetFavoriteCitiesAsync_ReturnsUserFavoriteCities()
        {

            var userId = "user1";

            _dbContext.FavoriteCities.AddRange(
                new FavoriteCity { UserId = userId, CityName = "Istanbul" },
                new FavoriteCity { UserId = userId, CityName = "Izmir" },
                new FavoriteCity { UserId = "user2", CityName = "Ankara" } 
            );
            await _dbContext.SaveChangesAsync();


            var favoriteCities = await _service.GetFavoriteCitiesAsync(userId);


            Assert.Equal(2, favoriteCities.Count());
            Assert.Contains(favoriteCities, fc => fc.CityName == "Istanbul");
            Assert.Contains(favoriteCities, fc => fc.CityName == "Izmir");
            Assert.DoesNotContain(favoriteCities, fc => fc.CityName == "Ankara");
        }

        [Fact]
        public async Task GetHottestCityAsync_ReturnsCityWithHighestTemperature()
        {

            var userId = "user1";

            _dbContext.FavoriteCities.AddRange(
                new FavoriteCity { UserId = userId, CityName = "Istanbul" },
                new FavoriteCity { UserId = userId, CityName = "Izmir" },
                new FavoriteCity { UserId = userId, CityName = "Ankara" }
            );
            await _dbContext.SaveChangesAsync();

            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Istanbul"))
                               .ReturnsAsync(20.0);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Izmir"))
                               .ReturnsAsync(25.0);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Ankara"))
                               .ReturnsAsync(18.0);


            var hottestCity = await _service.GetHottestCityAsync(userId);


            Assert.NotNull(hottestCity);
            Assert.Equal("Izmir", hottestCity.CityName);
        }

        [Fact]
        public async Task GetColdestCityAsync_ReturnsCityWithLowestTemperature()
        {

            var userId = "user1";

            _dbContext.FavoriteCities.AddRange(
                new FavoriteCity { UserId = userId, CityName = "Istanbul" },
                new FavoriteCity { UserId = userId, CityName = "Izmir" },
                new FavoriteCity { UserId = userId, CityName = "Ankara" }
            );
            await _dbContext.SaveChangesAsync();

            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Istanbul"))
                               .ReturnsAsync(20.0);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Izmir"))
                               .ReturnsAsync(25.0);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Ankara"))
                               .ReturnsAsync(18.0);


            var coldestCity = await _service.GetColdestCityAsync(userId);


            Assert.NotNull(coldestCity);
            Assert.Equal("Ankara", coldestCity.CityName);
        }

        [Fact]
        public async Task GetHottestCityAsync_ReturnsNull_WhenNoFavoriteCities()
        {

            var userId = "user1";


            var hottestCity = await _service.GetHottestCityAsync(userId);


            Assert.Null(hottestCity);
        }

        [Fact]
        public async Task GetColdestCityAsync_ReturnsNull_WhenNoFavoriteCities()
        {

            var userId = "user1";


            var coldestCity = await _service.GetColdestCityAsync(userId);


            Assert.Null(coldestCity);
        }

        [Fact]
        public async Task GetHottestCityAsync_ReturnsNull_WhenAllTemperaturesAreNull()
        {

            var userId = "user1";

            _dbContext.FavoriteCities.AddRange(
                new FavoriteCity { UserId = userId, CityName = "Istanbul" },
                new FavoriteCity { UserId = userId, CityName = "Izmir" }
            );
            await _dbContext.SaveChangesAsync();

            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Istanbul"))
                               .ReturnsAsync((double?)null);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Izmir"))
                               .ReturnsAsync((double?)null);


            var hottestCity = await _service.GetHottestCityAsync(userId);


            Assert.Null(hottestCity);
        }

        [Fact]
        public async Task GetColdestCityAsync_ReturnsNull_WhenAllTemperaturesAreNull()
        {

            var userId = "user1";

            _dbContext.FavoriteCities.AddRange(
                new FavoriteCity { UserId = userId, CityName = "Istanbul" },
                new FavoriteCity { UserId = userId, CityName = "Izmir" }
            );
            await _dbContext.SaveChangesAsync();

            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Istanbul"))
                               .ReturnsAsync((double?)null);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Izmir"))
                               .ReturnsAsync((double?)null);


            var coldestCity = await _service.GetColdestCityAsync(userId);


            Assert.Null(coldestCity);
        }

        [Fact]
        public async Task GetHottestCityAsync_ReturnsCityWithHighestTemperature_WhenTemperaturesAreSame()
        {

            var userId = "user1";

            _dbContext.FavoriteCities.AddRange(
                new FavoriteCity { UserId = userId, CityName = "Istanbul" },
                new FavoriteCity { UserId = userId, CityName = "Izmir" },
                new FavoriteCity { UserId = userId, CityName = "Ankara" }
            );
            await _dbContext.SaveChangesAsync();

            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Istanbul"))
                               .ReturnsAsync(20.0);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Izmir"))
                               .ReturnsAsync(20.0);
            _weatherManagerMock.Setup(m => m.GetAverageTemperatureAsync("Ankara"))
                               .ReturnsAsync(20.0);


            var hottestCity = await _service.GetHottestCityAsync(userId);


            Assert.NotNull(hottestCity);
            Assert.Contains(hottestCity.CityName, new[] { "Istanbul", "Izmir", "Ankara" });
        }
    }
}
