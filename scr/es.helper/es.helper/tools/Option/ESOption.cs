using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Option
{
    /// <summary>
    /// ES参数公共基类
    /// </summary>
    public class ESOption
    {
        /// <summary>
        /// 索引库名(多个索引名称用逗号隔开 eg: index_a,index_b)
        /// </summary>
        public string IndexName { get; set; }
        /// <summary>
        /// Document集合名(多个集合名称用逗号隔开 eg:type_a,type_b)
        /// </summary>
        public string TypeName { get; set; }
        #region +Analyzer Attr
        private string analyzer;
        /// <summary>
        /// ik_smart/ik_max_word
        /// </summary>
        public string Analyzer
        {
            get
            {
                if (string.IsNullOrEmpty(analyzer))
                {
                    analyzer = "ik_max_word";
                }
                return analyzer;
            }
            set
            {
                analyzer = value;
            }
        }    //查询分词器 
        #endregion
    }
}
