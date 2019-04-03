﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Entity
{
    /// <summary>
    /// ES搜索结果分页类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ESPageInfo<T> where T : class, new()
    {
        /// <summary>
        /// 数据总数
        /// </summary>
        public int DataCount { get; set; }
        /// <summary>
        /// 当前页码
        /// </summary>
        public int PageIndex { get; set; }
        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; }
        /// <summary>
        /// 总页数
        /// </summary>
        public int PageCount
        {
            get
            {
                PageSize = PageSize <= 0 ? 50 : PageSize;
                return  Convert.ToInt32(Math.Ceiling((double)DataCount / PageSize));
            }
        }
        /// <summary>
        /// 数据源列表
        /// </summary>
        public List<ESModel<T>> DataSource { get; set; }

    }
}
