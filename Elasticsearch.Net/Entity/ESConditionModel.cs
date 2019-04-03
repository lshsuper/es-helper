using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace  Common.Elasticsearch.Net.Entity
{
    /// <summary>
    /// Es查询条件
    /// </summary>
    public class ESConditionModel
    {
        /// <summary>
        ///必须闭合
        /// </summary>
        public List<QueryContainer> Must { get; set; }
        /// <summary>
        ///可以符合
        /// </summary>
        public List<QueryContainer> Should { get; set; }
        /// <summary>
        ///必须不符合
        /// </summary>
        public List<QueryContainer> MustNot { get; set; }
    }
}
