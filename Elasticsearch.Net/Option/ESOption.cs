using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Elasticsearch.Net.Option
{
    /// <summary>
    /// ES参数公共基类
    /// </summary>
   public class ESOption
    {
        /// <summary>
        /// 索引库名
        /// </summary>
        public string IndexName { get; set; }
        /// <summary>
        /// Document集合名
        /// </summary>
        public string TypeName { get; set; }
    }
}
