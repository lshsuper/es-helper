﻿using Common.Elasticsearch.Net.Entity;
using Common.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Elasticsearch.Net.Option
{
    /// <summary>
    /// 删除参数
    /// </summary>
    public class ESDeleteOption : ESOption
    {
        /// <summary>
        /// 条件
        /// </summary>
        public List<ESField> Where { get; set; }

    }
}
