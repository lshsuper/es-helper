using System;
using System.Collections.Generic;
using System.Text;

namespace es.helper.tools
{
  public  class ESHelper<T> where T:class,new()
    {
        private static object _lockEs = new object();
        private static ESContext<T> _context;
        /// <summary>
        /// 单实例
        /// </summary>
        public static ESContext<T> Instance
        {
            get
            {
                if (_context != null)
                    return _context;
                lock (_lockEs)
                {
                    if (_context != null)
                        return _context;
                    _context = new ESContext<T>();
                }
                return _context;
            }
        }
    }
}
