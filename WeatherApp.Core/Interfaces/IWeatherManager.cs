using System.Threading.Tasks;

namespace WeatherApp.Core.Interfaces
{
    public interface IWeatherManager
    {
        Task<double?> GetAverageTemperatureAsync(string location);
    }
}
