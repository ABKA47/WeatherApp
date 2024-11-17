using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WeatherApp.Core.Interfaces;
using WeatherApp.Core.Models;
using WeatherApp.Data;

namespace WeatherApp.Services
{
    public class WeatherManager : IWeatherManager
    {
        private readonly IEnumerable<IWeatherService> _weatherServices;
        private readonly IDistributedCache _cache;
        private readonly ILogger<WeatherManager> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly TimeSpan _groupDelay;

        private readonly ConcurrentDictionary<string, WeatherGroup> _weatherGroups = new ConcurrentDictionary<string, WeatherGroup>();

        public WeatherManager(IEnumerable<IWeatherService> weatherServices, IDistributedCache cache, ILogger<WeatherManager> logger, ApplicationDbContext dbContext, TimeSpan? groupDelay = null)
        {
            _weatherServices = weatherServices;
            _cache = cache;
            _logger = logger;
            _dbContext = dbContext;
            _groupDelay = groupDelay ?? TimeSpan.FromSeconds(5);
        }

        public async Task<double?> GetAverageTemperatureAsync(string location)
        {
            location = location.ToLower().Trim();
            var cacheKey = $"Weather_{location}";

            var cachedData = await _cache.GetAsync(cacheKey);
            if (cachedData != null)
            {
                var cachedString = System.Text.Encoding.UTF8.GetString(cachedData);
                _logger.LogInformation($"Önbellekten hava durumu alındı: {cachedString}°C: {location}");
                if (double.TryParse(cachedString, NumberStyles.Any, CultureInfo.InvariantCulture, out var cachedTemp))
                {
                    return cachedTemp;
                }
            }

            var cachedFromDb = await _dbContext.WeatherCache
                .Where(w => w.Location.ToLower() == location)
                .OrderByDescending(w => w.Timestamp)
                .FirstOrDefaultAsync();

            if (cachedFromDb != null)
            {
                _logger.LogInformation($"Veritabanından hava durumu alındı: {cachedFromDb.Temperature}°C: {location}");

                var cacheValue = System.Text.Encoding.UTF8.GetBytes(cachedFromDb.Temperature.ToString(CultureInfo.InvariantCulture));
                await _cache.SetAsync(cacheKey, cacheValue, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return cachedFromDb.Temperature;
            }

            var group = _weatherGroups.GetOrAdd(location, loc => new WeatherGroup(loc, RemoveGroup, this, _groupDelay));

            var averageTemp = await group.AddRequestAsync();

            return averageTemp;
        }

        private void RemoveGroup(string location)
        {
            _weatherGroups.TryRemove(location, out _);
        }

        private class WeatherGroup
        {
            private readonly string _location;
            private readonly Action<string> _removeGroupCallback;
            private readonly WeatherManager _manager;
            private readonly List<TaskCompletionSource<double?>> _requests;
            private readonly object _lock = new object();
            private readonly TimeSpan _delay;

            public WeatherGroup(string location, Action<string> removeGroupCallback, WeatherManager manager, TimeSpan delay)
            {
                _location = location;
                _removeGroupCallback = removeGroupCallback;
                _manager = manager;
                _delay = delay;
                _requests = new List<TaskCompletionSource<double?>>();

                Task.Run(async () =>
                {
                    await Task.Delay(_delay);
                    await ProcessRequestsAsync();
                    _removeGroupCallback(location);
                });
            }

            public Task<double?> AddRequestAsync()
            {
                var tcs = new TaskCompletionSource<double?>();
                lock (_lock)
                {
                    _requests.Add(tcs);
                }
                return tcs.Task;
            }
            private async Task ProcessRequestsAsync()
            {
                try
                {
                    var temperatures = new List<double>();

                    foreach (var service in _manager._weatherServices)
                    {
                        try
                        {
                            var result = await service.GetWeatherAsync(_location);
                            temperatures.Add(result.Temperature);
                        }
                        catch (Exception ex)
                        {
                            _manager._logger.LogWarning(ex, $"Servis başarısız: {service.GetType().Name} : {_location}");
                        }
                    }

                    double? averageTemp = null;

                    if (temperatures.Any())
                    {
                        averageTemp = temperatures.Average();

                        var cacheKey = $"Weather_{_location}";
                        var cacheValue = System.Text.Encoding.UTF8.GetBytes(averageTemp.Value.ToString(CultureInfo.InvariantCulture));
                        await _manager._cache.SetAsync(cacheKey, cacheValue, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });

                        var weatherCache = new WeatherCache
                        {
                            Location = _location,
                            Temperature = averageTemp.Value,
                            Timestamp = DateTime.UtcNow
                        };
                        _manager._dbContext.WeatherCache.Add(weatherCache);
                        await _manager._dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        _manager._logger.LogWarning($"Her iki servis de başarısız oldu: {_location}");
                    }

                    lock (_lock)
                    {
                        foreach (var tcs in _requests)
                        {
                            tcs.SetResult(averageTemp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _manager._logger.LogError(ex, $"WeatherGroup işleme hatası oluştu: {_location}");
                    lock (_lock)
                    {
                        foreach (var tcs in _requests)
                        {
                            tcs.SetResult(null);
                        }
                    }
                }
            }

        }
    }
}
