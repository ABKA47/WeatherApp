using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace WeatherApp.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<FavoriteCity> FavoriteCities { get; set; }
    }
}
