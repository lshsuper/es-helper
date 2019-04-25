using es.helper.tools.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Option
{

    /// <summary>
    /// 联想词汇展示列表
    /// </summary>
   public class ESSuggestionOption:ESOption
    {
        /// <summary>
        /// 建议字段
        /// </summary>
        public List<ESSuggestField> Suggests { get; set; }
        public bool IsHighLight { get; set; }
        public string PreTag { get; set; }
        public string PostTag { get; set; }
        public string Prefix { get; set; }

    }
}
