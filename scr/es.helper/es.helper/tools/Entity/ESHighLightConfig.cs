using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Entity
{
    /// <summary>
    /// 高亮配置
    /// </summary>
    public class ESHighLightConfig
    {
       
        /// <summary>
        /// 高亮标记前缀
        /// </summary>
        public string[] PreTags { get; set; }
        /// <summary>
        /// 高亮标记后缀
        /// </summary>
        public string[] PostTags { get; set; }
    }
}
