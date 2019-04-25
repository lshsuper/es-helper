using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools.Entity
{
    public class ESSuggestField
    {
        public string SuggestKey { get; set; }
     
        public int Size { get; set; }
        public bool Distinct { get; set; }   //是否去重
    }
}
