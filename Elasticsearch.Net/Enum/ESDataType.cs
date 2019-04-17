﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Elasticsearch.Net.Enum
{
    /// <summary>
    /// ES数据分类
    /// </summary>
    public enum ESDataType
    {
        /// <summary>
        /// 文本类型
        /// </summary>
        Text = 0,
        /// <summary>
        /// 数字类型
        /// </summary>
        Number = 1,
        /// <summary>
        ///日期类型
        /// </summary>
        Date = 2,
        /// <summary>
        /// 关键词类型
        /// </summary>
        Keyword = 3
    }
}