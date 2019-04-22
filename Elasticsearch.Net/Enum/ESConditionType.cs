using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiuLiao.Common.Elasticsearch.Net.Enum
{
    /// <summary>
    /// ES条件类别
    /// </summary>
    public enum ESConditionType
    {
        Must=0,
        Should=1,
        MustNot=2
    }
}
