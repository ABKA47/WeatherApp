using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using WeatherApp.Core.Interfaces;

namespace WeatherApp.Services
{
    public class WeatherApiComService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<WeatherApiComService> _logger;

        public WeatherApiComService(HttpClient httpClient, IConfiguration configuration, ILogger<WeatherApiComService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["WeatherApiCom:ApiKey"];
            _logger = logger;
        }

        public async Task<WeatherResult> GetWeatherAsync(string location)
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://api.weatherapi.com/v1/current.json?key={_apiKey}&q={location}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(content);
                double tempC = data["current"]["temp_c"].Value<double>();

                return new WeatherResult
                {
                    Temperature = tempC,
                    Source = "WeatherAPI.com"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"WeatherApiComService hata oluştu: {location}");
                throw;
            }
        }
    }
}
