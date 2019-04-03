using JiuLiao.Common.Elasticsearch.Net.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Elasticsearch.Net.Entity
{
    public class ESField
    {

        /// <summary>
        ///字段类型
        /// </summary>
        public ESDataType DataType { get; set; }
        /// <summary>
        /// 查询类型
        /// </summary>
        public ESQueryType QueryType { get; set; }
        /// <summary>
        /// 字段名
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public Tuple<object, object> Value { get; set; }
        /// <summary>
        /// 条件种类
        /// </summary>
        public ESConditionType ConditionType { get; set; }



    }
}
