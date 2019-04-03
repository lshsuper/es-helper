using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Entity
{
    /// <summary>
    /// ES单个Document数据信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ESModel<T> where T : class, new()
    {
        public T Data { get; set; }   //程序实体对象映射
        public string UniqueId { get; set; } //ES动态生成的UUID映射 or 自定义的唯一键值
    }
}
