using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiuLiao.Common.Entity
{
    /// <summary>
    /// ES单个Document数据信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ESModel<T> where T : class, new()
    {
        public T Data { get; set; }   //程序实体对象映射
        public string UniqueId { get; set; } //ES动态生成的UUID映射 or 自定义的唯一键值

        public double? Score { get; set; }   //匹配程度分数
        public string IndexName { get; set; }   //当前的文档属于哪个索引
        public string TypeName { get; set; }  //当前的文档属于哪个类型
    }
}
