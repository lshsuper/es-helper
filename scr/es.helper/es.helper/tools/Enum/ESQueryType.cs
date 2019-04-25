using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Enum
{
    /// <summary>
    ///匹配方式
    /// </summary>
    public enum ESQueryType
    {

        /// <summary>
        /// 模糊匹配
        /// </summary>
        Match = 0,
        /// <summary>
        /// 全量匹配
        /// </summary>
        Term = 1,
        /// <summary>
        /// 范围匹配
        /// </summary>
        Range = 2,
        /// <summary>
        ///所有记录的匹配
        /// </summary>
        All = 3,
        #region  About Geo Point
        /// <summary>
        ///边框范围(topleft,bottomright--左上角与右上角确定的区域)
        /// </summary>
        Geo_Bounding_Box = 4,
        /// <summary>
        /// 指定多点组成的多边形，获取区域内的点
        /// </summary>
        Geo_Polygon = 5,
        /// <summary>
        ///指定半径做圆，获取区域内的点
        /// </summary>
        Geo_Distance = 6,
        #endregion
        /// <summary>
        /// 短语匹配
        /// </summary>
        Match_Phrase=7,
        /// <summary>
        /// 短语前缀匹配
        /// </summary>
        Match_Phrase_Prefix=8


    }
}
