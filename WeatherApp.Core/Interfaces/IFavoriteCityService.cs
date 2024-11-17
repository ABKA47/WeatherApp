using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherApp.Core.Models;

namespace WeatherApp.Core.Interfaces
{
    public interface IFavoriteCityService
    {
        Task<IEnumerable<FavoriteCity>> GetFavoriteCitiesAsync(string userId);
        Task AddFavoriteCityAsync(string userId, string cityName);
        Task RemoveFavoriteCityAsync(string userId, int favoriteCityId);
        Task<FavoriteCity> GetHottestCityAsync(string userId);
        Task<FavoriteCity> GetColdestCityAsync(string userId);
    }
}
