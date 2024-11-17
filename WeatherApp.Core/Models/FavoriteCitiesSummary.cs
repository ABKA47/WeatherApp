using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherApp.Core.Models
{
    public class FavoriteCitiesSummary
    {
        public string HottestCity { get; set; }
        public double? HottestTemperature { get; set; }

        public string ColdestCity { get; set; }
        public double? ColdestTemperature { get; set; }
    }
}
