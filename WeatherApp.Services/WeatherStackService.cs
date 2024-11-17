using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using WeatherApp.Core.Interfaces;

namespace WeatherApp.Services
{
    public class WeatherStackService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<WeatherStackService> _logger;

        public WeatherStackService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherStackService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["WeatherStack:ApiKey"];
            _logger = logger;
        }

        public async Task<WeatherResult> GetWeatherAsync(string location)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://api.weatherstack.com/current?access_key={_apiKey}&query={location}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(content);
                double tempC = data["current"]["temperature"].Value<double>();

                return new WeatherResult
                {
                    Temperature = tempC,
                    Source = "WeatherStack"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WeatherStackService hata oluştu: {location}");
                throw;
            }
        }
    }
}
