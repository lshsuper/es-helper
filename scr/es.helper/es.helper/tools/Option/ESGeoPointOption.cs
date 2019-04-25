using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Option
{
   public class ESGeoPointOption
    {
       
    }
    public class ESGeoDistanceOption: ESGeoPointOption
    {
        /// <summary>
        /// 纬度
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// 经度
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// 半径（eg:1.2km）
        /// </summary>
        public string Distance { get; set; }
    }
    public class ESGeoPolygonOption: ESGeoPointOption
    {
          public List<Tuple<double, double>> Points { get; set; }
    }
}
