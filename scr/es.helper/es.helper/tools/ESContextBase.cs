using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace es.helper.tools
{
    /// <summary>
    /// 说明:
    /// 1.es基础类 （基础系列接口，与自定义扩展分离，方便方法扩展完善）
    /// 2.自定义实现时，es的客户端保持单例（即：全局使用_es）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ESContextBase<T> where T : class, new()
    {
        #region Context
        protected ElasticClient _es;   //Client
        /// <summary>
        /// 实例 
        /// </summary>
        public ESContextBase()
        {

            ConnectionSettings setting = new ConnectionSettings(new Uri(""));
            _es = new ElasticClient(setting);

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
        public bool Set(T model, string indexName, string typeName, string id)
        {

            var res = new IndexRequest<T>(indexName, typeName, id);
            res.Document = model;
            IndexResponse result = (IndexResponse)_es.Index<T>(res);
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
        public bool SetMuilt(IEnumerable<T> model, string indexName, string typeName)
        {

            BulkResponse result = (BulkResponse)_es.IndexMany<T>(model, indexName, typeName);
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
        public bool AddIndex(T model, string indexName, string typeName, string id)
        {
            if (!IndexExist(indexName))
            {
                InitializeIndexMap(indexName);
            }
            var res = new IndexRequest<T>(indexName, typeName, id);
            res.Document = model;
            IndexResponse result = (IndexResponse)_es.Index<T>(res);
            return result.IsValid;

        }
        /// <summary>
        /// 删除单一文档(by id)
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Delete(string indexName, string typeName, string id)
        {

            DeleteRequest request = new DeleteRequest(indexName, typeName, id);
            DeleteResponse response = (DeleteResponse)_es.Delete(request);
            return response.IsValid;


        }
        /// <summary>
        /// 更新（需要自己构建request对象）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool Update(UpdateByQueryRequest<T> request)
        {
            var res = _es.UpdateByQuery(request);
            return res.Updated > 0;


        }
        /// <summary>
        /// 删除（需要自己构建request对象）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public bool Delete(DeleteByQueryRequest<T> request)
        {

            var res = _es.DeleteByQuery(request);
            return res.Deleted > 0;

        }
        /// <summary>
        /// 搜索（需要自己构建request）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public List<T> Search(SearchRequest<T> request)
        {

            return _es.Search<T>(request).Documents.ToList();

        }
        /// <summary>
        /// 索引是否存在
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public bool IndexExist(string indexName)
        {
            IndexExistsRequest request = new IndexExistsRequest(indexName);
            var response = _es.IndexExists(request);
            return response.Exists;
        }
        /// <summary>
        ///     InitializeIndexMap
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="index"></param>
        public void InitializeIndexMap(string index)
        {
            var descriptor = new CreateIndexDescriptor(index)
                .Mappings(ms => ms
                    .Map<T>(m => m.AutoMap())
                )
                .Settings(s => s.NumberOfShards(5)
                .NumberOfReplicas(1));
            var response = _es.CreateIndex(descriptor);

            if (!response.IsValid)
                throw new Exception("新增Index:" + response.OriginalException.Message);
        }
        #endregion
    }
}
