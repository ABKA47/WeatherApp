using System.Threading.Tasks;

namespace WeatherApp.Core.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherResult> GetWeatherAsync(string location);
    }

    public class WeatherResult
    {
        public double Temperature { get; set; }
        public string Source { get; set; }
    }
}
