using Common.Elasticsearch.Net.Entity;
using Common.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Elasticsearch.Net.Option
{
   public class ESUpdateOption:ESOption
    {
        /// <summary>
        /// 条件
        /// </summary>
        public List<ESField> Where { get; set; }
        /// <summary>
        /// 待修改数据
        /// </summary>
        public Dictionary<string,object> Model { get; set; }
    }
}
