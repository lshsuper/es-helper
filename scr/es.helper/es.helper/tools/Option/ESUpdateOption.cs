
using es.helper.tools.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Option
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
