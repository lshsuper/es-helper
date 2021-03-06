﻿using es.helper.tools.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Option
{

    /// <summary>
    /// ES查询option
    /// </summary>
    public class ESSearchBaseOption : ESOption
    {
        /// <summary>
        /// 条件
        /// </summary>
        public List<ESField> Where { get; set; }
        /// <summary>
        /// 排序多个字段
        /// </summary>
        public Dictionary<string, bool> OrderDic { get; set; }
        /// <summary>
        ///当前页码
        /// </summary>
        private int pageIndex;
        public int PageIndex
        {
            get
            {
                if (pageIndex <= 0)
                {
                    return 1;
                }
                return pageIndex;
            }
            set
            {
                pageIndex = value;
            }
        }
        private int pageSize;
        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize
        {
            get
            {

                if (pageSize <= 0)
                {
                    return 20;
                }
                return pageSize;

            }
            set
            {

                pageSize = value;
            }
        }
        /// <summary>
        /// 高亮配置
        /// </summary>
        public ESHighLightConfig ESHighLightConfig { get; set; }
      
    }

    public class ESSearchOption:ESSearchBaseOption
    {

    }

    /// <summary>
    /// 分组查询参数
    /// </summary>
    public class ESSearchWithGroupOption : ESSearchBaseOption
    {
        /// <summary>
        /// 分组字段
        /// </summary>
        public HashSet<string> GroupBys { get; set; }
        /// <summary>
        /// 分组字段标签分隔符
        /// </summary>
        public string SplitTagForGroupBys { get; set; }
        /// <summary>
        /// 获取哪些字段
        /// </summary>
        public HashSet<string> Fields { get; set; }
    }
}
