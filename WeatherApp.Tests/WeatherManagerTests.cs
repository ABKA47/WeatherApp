using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Core.Interfaces;
using WeatherApp.Data;
using WeatherApp.Core.Models;
using WeatherApp.Services;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.Data.Sqlite; // Eklenen

namespace WeatherApp.Tests
{
    public class WeatherManagerTests : IDisposable
    {
        private readonly Mock<IWeatherService> _weatherService1Mock;
        private readonly Mock<IWeatherService> _weatherService2Mock;
        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ILogger<WeatherManager>> _loggerMock;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWeatherManager _manager;

        public WeatherManagerTests()
        {
            _weatherService1Mock = new Mock<IWeatherService>();
            _weatherService2Mock = new Mock<IWeatherService>();
            var weatherServices = new List<IWeatherService> { _weatherService1Mock.Object, _weatherService2Mock.Object };

            _cacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<WeatherManager>>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ApplicationDbContext(options);

            _manager = new WeatherManager(weatherServices, _cacheMock.Object, _loggerMock.Object, _dbContext, TimeSpan.FromSeconds(1));
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task GetAverageTemperatureAsync_ReturnsFromCache()
        {
            var location = "istanbul";
            var cacheKey = $"Weather_{location}";
            var cachedTemp = "20.5";
            var cachedBytes = Encoding.UTF8.GetBytes(cachedTemp);

            _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(cachedBytes);

            var result = await _manager.GetAverageTemperatureAsync(location);

            Assert.Equal(20.5, result);
            _weatherService1Mock.Verify(s => s.GetWeatherAsync(It.IsAny<string>()), Times.Never);
            _weatherService2Mock.Verify(s => s.GetWeatherAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAverageTemperatureAsync_ReturnsFromDatabaseAndUpdatesCache()
        {
            var location = "ankara";
            var cacheKey = $"Weather_{location}";

            _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);

            var weatherCache = new WeatherCache
            {
                Location = location,
                Temperature = 18.0,
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            };
            _dbContext.WeatherCache.Add(weatherCache);
            await _dbContext.SaveChangesAsync();

            _cacheMock.Setup(c => c.SetAsync(
    cacheKey,
    It.IsAny<byte[]>(),
    It.IsAny<DistributedCacheEntryOptions>(),
    It.IsAny<CancellationToken>()))
          .Returns(Task.CompletedTask);


            var result = await _manager.GetAverageTemperatureAsync(location);

            Assert.Equal(18.0, result);
            _weatherService1Mock.Verify(s => s.GetWeatherAsync(It.IsAny<string>()), Times.Never);
            _weatherService2Mock.Verify(s => s.GetWeatherAsync(It.IsAny<string>()), Times.Never);
            _cacheMock.Verify(c => c.SetAsync(
                cacheKey,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b, 0, b.Length) == "18"),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAverageTemperatureAsync_ReturnsFromApiAndUpdatesCacheAndDatabase()
        {

            var location = "izmir";
            var cacheKey = $"Weather_{location}";


            _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);


            _weatherService1Mock.Setup(s => s.GetWeatherAsync(location))
                                .ReturnsAsync(new WeatherResult { Temperature = 25.0, Source = "Service1" });

            _weatherService2Mock.Setup(s => s.GetWeatherAsync(location))
                                .ReturnsAsync(new WeatherResult { Temperature = 27.0, Source = "Service2" });

            _cacheMock.Setup(c => c.SetAsync(
                cacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            var result = await _manager.GetAverageTemperatureAsync(location);

            Assert.Equal(26.0, result);
            _cacheMock.Verify(c => c.SetAsync(
                cacheKey,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b, 0, b.Length) == "26"),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
            var cachedWeather = await _dbContext.WeatherCache.FirstOrDefaultAsync(w => w.Location == location);
            Assert.NotNull(cachedWeather);
            Assert.Equal(26.0, cachedWeather.Temperature);
        }


        [Fact]
        public async Task GetAverageTemperatureAsync_ReturnsFromApiWithMultipleRequests()
        {
            var location = "bursa";
            var cacheKey = $"Weather_{location}";

            _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);

            _weatherService1Mock.Setup(s => s.GetWeatherAsync(location))
                                .ReturnsAsync(new WeatherResult { Temperature = 22.0, Source = "Service1" });

            _weatherService2Mock.Setup(s => s.GetWeatherAsync(location))
                                .ReturnsAsync(new WeatherResult { Temperature = 24.0, Source = "Service2" });

            _cacheMock.Setup(c => c.SetAsync(
                cacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

            var task1 = _manager.GetAverageTemperatureAsync(location);
            var task2 = _manager.GetAverageTemperatureAsync(location);
            await Task.WhenAll(task1, task2);

            Assert.Equal(23.0, task1.Result);
            Assert.Equal(23.0, task2.Result);
            _cacheMock.Verify(c => c.SetAsync(
                cacheKey,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b, 0, b.Length) == "23"),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
            var cachedWeather = await _dbContext.WeatherCache.FirstOrDefaultAsync(w => w.Location == location);
            Assert.NotNull(cachedWeather);
            Assert.Equal(23.0, cachedWeather.Temperature);
        }

        [Fact]
        public async Task GetAverageTemperatureAsync_ReturnsNull_WhenAllServicesFail_And_NoCacheOrDbData()
        {
            var location = "kocaeli";
            var cacheKey = $"Weather_{location}";

            _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);

            _weatherService1Mock.Setup(s => s.GetWeatherAsync(location))
                                .ThrowsAsync(new Exception("Service1 failed"));

            _weatherService2Mock.Setup(s => s.GetWeatherAsync(location))
                                .ThrowsAsync(new Exception("Service2 failed"));

            var result = await _manager.GetAverageTemperatureAsync(location);

            Assert.Null(result);
            _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Her iki servis de başarısız oldu")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetAverageTemperatureAsync_ReturnsFromDb_WhenAllServicesFail_ButDbHasData()
        {
            var location = "sakarya";
            var cacheKey = $"Weather_{location}";

            _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((byte[])null);

            var weatherCache = new WeatherCache
            {
                Location = location,
                Temperature = 19.0,
                Timestamp = DateTime.UtcNow.AddMinutes(-10)
            };
            _dbContext.WeatherCache.Add(weatherCache);
            await _dbContext.SaveChangesAsync();

            _weatherService1Mock.Setup(s => s.GetWeatherAsync(location))
                     .ThrowsAsync(new Exception("Service1 failed"));

            _weatherService2Mock.Setup(s => s.GetWeatherAsync(location))
                                .ThrowsAsync(new Exception("Service2 failed"));


            var result = await _manager.GetAverageTemperatureAsync(location);


            Assert.Equal(19.0, result);
            _cacheMock.Verify(c => c.SetAsync(
                cacheKey,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b, 0, b.Length) == "19"),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Her iki servis de başarısız oldu")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never); // Log mesajı çağrılmamalı
        }
    }
}
