using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Elasticsearch.Net.Option
{
    public class ESSqlSearchOption
    {
        public string IndexName { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public string OrderByStr { get; private set; }
        public string FilterStr { get; private set; }
        public string Sql
        {

            get
            {
                return $"select * from {IndexName} {OrderByStr} {FilterStr} {BuildRangeStr(PageIndex,PageSize)} ";
            }
        }
        /// <summary>
        /// 构建order by
        /// </summary>
        /// <param name="column"></param>
        /// <param name="isDesc"></param>
        public void BuildOrderByStr(string column, bool isDesc = true)
        {
            OrderByStr = $"order by {column} {(isDesc ? "desc" : "asc")}";
        }
        /// <summary>
        /// 构建 filter
        /// </summary>
        /// <param name="str"></param>
        public void PutFilterStr(string str)
        {
            if (string.IsNullOrEmpty(FilterStr))
            {
                FilterStr = $"where ({str})";
            }
            else
            {
                FilterStr += $" and ({str})";
            }
        }
        /// <summary>
        /// 分页 range
        /// </summary>
        /// <param name="column"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        private string BuildRangeStr(int pageIndex,int pageSize)
        {
             return $" limit {(pageIndex-1)*pageSize},{pageSize}";
        }
    }
}
