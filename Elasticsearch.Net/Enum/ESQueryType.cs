using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Elasticsearch.Net.Enum
{
    /// <summary>
    ///匹配方式
    /// </summary>
    public enum ESQueryType
    {

       /// <summary>
       /// 模糊匹配
       /// </summary>
       Match=0,
       /// <summary>
       /// 全量匹配
       /// </summary>
       Term=1,
       /// <summary>
       /// 范围匹配
       /// </summary>
       Range=2,
       /// <summary>
       ///所有记录的匹配
       /// </summary>
       All=3


    }
}
