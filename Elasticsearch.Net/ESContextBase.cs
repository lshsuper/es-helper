using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiuLiao.Common.Elasticsearch.Net.Option
{
    /// <summary>
    /// 说明:
    /// 1.es基础类 （基础系列接口，与自定义扩展分离，方便方法扩展完善）
    /// 2.自定义实现时，es的客户端保持单例（即：全局使用GetInstance）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ESContextBase<T> where T:class,new()
    {
        #region Context
        private static object _lock = new object();
        private static ElasticClient _es;   //Client
        /// <summary>
        /// 实例 
        /// </summary>
        public static ElasticClient GetInstance
        {

            get
            {
                if (_es != null)
                {
                    return _es;
                }
                lock (_lock)
                {
                    if (_es != null)
                    {
                        return _es;
                    }
                    ConnectionSettings setting = new ConnectionSettings(new Uri(Constant.ElasticSearchConn));
                    _es = new ElasticClient(setting);
                    return _es;
                }

            }
        } 
        #endregion

        #region Basic Operate
        /// <summary>
        /// 单一添加一行数据(没有就创建,有就修改覆盖)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Set(T model, string indexName, string typeName, string id)
        {

            var res = new IndexRequest<T>(indexName, typeName, id);
            res.Document = model;
            IndexResponse result = (IndexResponse)GetInstance.Index<T>(res);
            return result.IsValid;
        }
        /// <summary>
        /// 批量添加(没有就创建,有就修改覆盖)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static bool SetMuilt(IEnumerable<T> model, string indexName, string typeName)
        {

            BulkResponse result = (BulkResponse)GetInstance.IndexMany<T>(model, indexName, typeName);
            return result.IsValid;
        }
        /// <summary>
        /// 根据实体映射，初始化Index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool AddIndex(T model, string indexName, string typeName, string id)
        {
            if (!IndexExist(indexName))
            {
                InitializeIndexMap(indexName);
            }
            var res = new IndexRequest<T>(indexName, typeName, id);
            res.Document = model;
            IndexResponse result = (IndexResponse)GetInstance.Index<T>(res);
            return result.IsValid;
            
        }
        /// <summary>
        /// 删除单一文档(by id)
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Delete(string indexName, string typeName, string id)
        {

            DeleteRequest request = new DeleteRequest(indexName, typeName, id);
            DeleteResponse response = (DeleteResponse)GetInstance.Delete(request);
            return response.IsValid;


        }
        /// <summary>
        /// 更新（需要自己构建request对象）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool Update(UpdateByQueryRequest<T> request)
        {
            var res = GetInstance.UpdateByQuery(request);
            return res.Updated > 0;


        }
        /// <summary>
        /// 删除（需要自己构建request对象）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool Delete(DeleteByQueryRequest<T> request)
        {

            var res = GetInstance.DeleteByQuery(request);
            return res.Deleted > 0;

        }
        /// <summary>
        /// 搜索（需要自己构建request）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static List<T> Search(SearchRequest<T> request)
        {

            return GetInstance.Search<T>(request).Documents.ToList();

        }
        /// <summary>
        /// 索引是否存在
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public static bool IndexExist(string indexName)
        {
            IndexExistsRequest request = new IndexExistsRequest(indexName);
            var response = GetInstance.IndexExists(request);
            return response.Exists;
        }
        /// <summary>
        ///     InitializeIndexMap
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="index"></param>
        public static void InitializeIndexMap(string index)
        {
            var descriptor = new CreateIndexDescriptor(index)
                .Mappings(ms => ms
                    .Map<T>(m => m.AutoMap())
                )
                .Settings(s => s.NumberOfShards(5)
                .NumberOfReplicas(1));
            var response = GetInstance.CreateIndex(descriptor);

            if (!response.IsValid)
                throw new Exception("新增Index:" + response.OriginalException.Message);
        }
        #endregion
    }
}
