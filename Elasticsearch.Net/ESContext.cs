using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Common;
using Common.Elasticsearch.Net.Entity;
using Common.Elasticsearch.Net.Enum;
using Common.Elasticsearch.Net.Option;
using Common.Entity;
using Nest;
using static System.Net.Mime.MediaTypeNames;

namespace Common.Elasticsearch.Net
{
    /// <summary>
    /// ES Context
    /// Sql相关接口需要安装【elasticsearch-sql-x】插件才可以使用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ESContext<T> where T : class, new()
    {
        public static readonly ElasticClient _es;   //Client
        static ESContext()
        {
            ConnectionSettings setting = new ConnectionSettings(new Uri(Constant.ElasticSearchConn));
            _es = new ElasticClient(setting);
        }

        #region Context-- Util

        #region Util-- Document CURD
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
        public static bool SetMuilt(IEnumerable<T> model, string indexName, string typeName)
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
        public static bool AddIndex(T model, string indexName, string typeName, string id)
        {

            if (!IndexExist(indexName))
            {
                ESIndexMap.InitializeIndexMap<T>(_es, indexName);
            }

            var res = new IndexRequest<T>(indexName, typeName, id);
            res.Document = model;
            IndexResponse result = (IndexResponse)_es.Index<T>(res);
            return result.IsValid;


        }
        /// <summary>
        /// 查询列表（by page）
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static ESPageInfo<T> Query(ESSearchOption option)
        {

            ISearchRequest searchRequest = new SearchRequest<T>(option.IndexName, option.TypeName);
            var data = new ESPageInfo<T>();
            var queryCondations = BuildQueryContainer(option.Where);
            var shouldQuerys = queryCondations.Should;
            var mustQuerys = queryCondations.Must;
            var mustNot = queryCondations.MustNot;
            #region 多条件模糊匹配
            searchRequest.Query = new BoolQuery()
            {
                Should = shouldQuerys,
                Must = mustQuerys,
                MustNot = mustNot
            };
            if (option.ESHighLightConfig != null)
            {
                searchRequest.Highlight = BuildHightLight(option.Where.Select(o => o.Key).ToList(), option.ESHighLightConfig);
            }
            #endregion
            #region 多字段排序
            if (option.OrderDic != null)
            {
                List<ISort> orderFileds = new List<ISort>();
                foreach (var item in option.OrderDic)
                {
                    orderFileds.Add(new SortField()
                    {
                        Field = item.Key,
                        Order = item.Value ? SortOrder.Descending : SortOrder.Ascending,

                    });
                }
                searchRequest.Sort = orderFileds;
            }
            #endregion
            searchRequest.From = (option.PageIndex - 1) * option.PageSize;
            searchRequest.Size = option.PageSize;
            var result = _es.Search<T>(searchRequest);
            data.DataCount = Convert.ToInt32(result.Total);
            data.PageIndex = option.PageIndex;
            data.PageSize = option.PageSize;
            data.DataSource = new List<ESModel<T>>();
            //判断时候需要改良匹配
            if (option.ESHighLightConfig == null)
            {
                data.DataSource = result.Hits.Select(o =>
                          new ESModel<T>()
                          {
                              UniqueId = o.Id,
                              Data = o.Source
                          }).ToList();

            }
            else
            {
                IReadOnlyCollection<IHit<T>> hits = result.Hits;  //找到高亮列表
                foreach (var hit in hits)
                {
                    var currentHightList = hit.Highlights;
                    var sourceProps = hit.Source.GetType().GetProperties();
                    foreach (var sourceProp in sourceProps)
                    {
                        KeyValuePair<string, HighlightHit> currentMatch = currentHightList.FirstOrDefault(o => o.Key == sourceProp.Name);
                        if (currentMatch.Key != null)
                        {
                            sourceProp.SetValue(hit.Source, Convert.ChangeType(currentMatch.Value.Highlights.First(), sourceProp.PropertyType));
                        }
                    }
                    data.DataSource.Add(new ESModel<T>()
                    {

                        Data = hit.Source,
                        UniqueId = hit.Id
                    });
                }
            }
            return data;




        }
        /// <summary>
        /// 自动补全列表（配合Completion类型的字段进行使用）
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static List<ESSuggesModel> Suggest(ESSuggestionOption option)
        {
            SearchRequest request = new SearchRequest(option.IndexName, option.TypeName);
            List<ESSuggesModel> model = new List<ESSuggesModel>();
            request.Suggest = new SuggestContainer();
            foreach (var suggest in option.Suggests)
            {

                request.Suggest.Add($"suggest_{suggest.SuggestKey}", new SuggestBucket()
                {
                    Prefix = option.Prefix,
                    Completion = new CompletionSuggester()
                    {
                        Field = suggest.SuggestKey,
                        Size = suggest.Size,
                        SkipDuplicates = suggest.Distinct
                    },
                });
            }
            var result = _es.Search<T>(request);
            SuggestDictionary<T> suggestDic = result.Suggest;
            foreach (var item in suggestDic)
            {
                Suggest<T>[] val = item.Value as Suggest<T>[];
                if (val != null && val.Count() > 0)
                {
                    foreach (var opt in val[0].Options)
                    {
                        string currentTxt = opt.Text;
                        if (option.IsHighLight)
                        {
                            currentTxt = BuildHightLightString(currentTxt, option.Prefix, option.PreTag, option.PostTag);
                        }
                        model.Add(new ESSuggesModel()
                        {
                            Text = currentTxt,
                            FromKey = item.Key.Split('_')[1]
                        });
                    }
                }
            }
            return model;
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
            DeleteResponse response = (DeleteResponse)_es.Delete(request);
            return response.IsValid;


        }
        /// <summary>
        /// 修改指定的字段(by id)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Update(Dictionary<string, object> model, string indexName, string typeName, string id)
        {

            DocumentPath<T> deletePath = new DocumentPath<T>(id);
            IUpdateRequest<T, object> request = new UpdateRequest<T, object>(deletePath, indexName, typeName)
            {
                Doc = model
            };
            var response = _es.Update(request);
            return response.IsValid;


        }
        /// <summary>
        /// 修改指全部字段(by id)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Update(T model, string indexName, string typeName, string id)
        {

            DocumentPath<T> deletePath = new DocumentPath<T>(id);
            IUpdateRequest<T, object> request = new UpdateRequest<T, object>(deletePath, indexName, typeName)
            {
                Doc = model
            };
            var response = _es.Update(request);
            return response.IsValid;

        }
        /// <summary>
        /// 修改
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Update(ESUpdateOption option)
        {


            var queryCondations = BuildQueryContainer(option.Where);
            var shouldQuerys = queryCondations.Should;
            var mustQuerys = queryCondations.Must;
            var mustNotQuerys = queryCondations.MustNot;
            if (mustQuerys.Count <= 0 && shouldQuerys.Count <= 0 && mustNotQuerys.Count <= 0)
            {
                return false;
            }
            UpdateByQueryRequest<T> request = new UpdateByQueryRequest<T>(option.IndexName, option.TypeName);
            request.Script.Params = option.Model;

            var query = new BoolQuery();

            if (mustQuerys.Count > 0)
            {
                query.Must = mustQuerys;
            }
            if (shouldQuerys.Count > 0)
            {
                query.Should = shouldQuerys;
            }
            if (mustNotQuerys.Count > 0)
            {
                query.MustNot = mustNotQuerys;
            }
            request.Query = query;

            var res = _es.UpdateByQuery(request);
            return res.Updated > 0;



        }
        /// <summary>
        /// 返回单条(by id)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="mustDic"></param>
        /// <returns></returns>
        public static ESModel<T> Find(string indexName, string typeName, string id)
        {

            var request = new GetRequest<T>(indexName, typeName, id);
            var result = new ESModel<T>();
            var response = _es.Get<T>(request);
            if (response.IsValid)
            {
                result.UniqueId = response.Id;
            }
            return result;


        }
        /// <summary>
        /// 热门词汇列表（{“分组值”，“当前文档的数量”}）
        /// </summary>
        /// <param name="indexName"></param>
        /// <param name="typeName"></param>
        /// <param name="groupBy"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Dictionary<string, long> Group(string indexName, string typeName, string groupBy, int size = 10)
        {
            var result = _es.Search<T>(s => s.Index(indexName).Type(typeName)
                            .Aggregations(ag => ag
                                    .Terms($"group_by_{groupBy}", t => t.Field(groupBy).Size(size))//分组
                                )
                            );
            SearchRequest request = new SearchRequest();
            var hotDic = new Dictionary<string, long>();
            IAggregate aggr;
            result.Aggregations.TryGetValue($"group_by_{groupBy}", out aggr);
            if (aggr != null)
            {
                BucketAggregate bucket = aggr as BucketAggregate;
                if (bucket != null && bucket.Items.Count > 0)
                {
                    foreach (KeyedBucket<object> item in bucket.Items)
                    {
                        hotDic.Add(item.Key.ToString(), item.DocCount ?? 0);
                    }
                }

            }
            return hotDic;
        }
        /// <summary>
        /// 按照指定条件删除
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static bool Delete(ESDeleteOption option)
        {
            var queryCondations = BuildQueryContainer(option.Where);
            var shouldQuerys = queryCondations.Should;
            var mustQuerys = queryCondations.Must;
            var mustNotQuerys = queryCondations.MustNot;
            if (mustQuerys.Count <= 0 && shouldQuerys.Count <= 0 && mustNotQuerys.Count <= 0)
            {
                return false;
            }

            var request = new DeleteByQueryRequest<T>(option.IndexName, option.TypeName);
            var query = new BoolQuery();

            if (mustQuerys.Count > 0)
            {
                query.Must = mustQuerys;
            }
            if (shouldQuerys.Count > 0)
            {
                query.Should = shouldQuerys;
            }
            if (mustNotQuerys.Count > 0)
            {
                query.MustNot = mustNotQuerys;
            }
            request.Query = query;

            var res = _es.DeleteByQuery(request);
            return res.Deleted > 0;
        }
        /// <summary>
        /// 更新（需要自己构建request对象）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool Update(UpdateByQueryRequest<T> request)
        {
            var res = _es.UpdateByQuery(request);
            return res.Updated > 0;


        }
        /// <summary>
        /// 删除（需要自己构建request对象）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool Delete(DeleteByQueryRequest<T> request)
        {

            var res = _es.DeleteByQuery(request);
            return res.Deleted > 0;

        }
        /// <summary>
        /// 搜索（需要自己构建request）
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static List<T> Search(SearchRequest<T> request)
        {

            return _es.Search<T>(request).Documents.ToList();

        }
        /// <summary>
        /// 使用sql语句查询(含分页)
        /// </summary>
        /// <param name="eSContext"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static ESPageInfo<T> QueryBySql(ESSqlSearchOption option)
        {
            var data = new ESPageInfo<T>();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constant.ElasticSearchConn);

                StringContent content = new StringContent(JsonHelper.Json(new { sql = option.Sql }), Encoding.UTF8, "application/json");
                HttpResponseMessage msg = client.PostAsync("_sql", content).Result;
                if (msg.StatusCode != HttpStatusCode.OK)
                {
                    return data;
                }

                string result = msg.Content.ReadAsStringAsync().Result;
                dynamic response = JsonHelper.DeserializeJson<dynamic>(result);
                data.DataCount = Convert.ToInt32(response.hits.total);
                data.PageIndex = option.PageIndex;
                data.PageSize = option.PageSize;
                List<dynamic> hits = JsonHelper.DeserializeJson<List<dynamic>>(response.hits.hits.ToString());
                data.DataSource = hits.Select(o =>
                     new ESModel<T>()
                     {
                         UniqueId = o._id.ToString(),
                         Data = JsonHelper.DeserializeJson<T>(o._source.ToString())
                     }
                ).ToList();
                return data;
            }
        }
        /// <summary>
        /// 使用sql语句查询
        /// </summary>
        /// <param name="eSContext"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static List<ESModel<T>> QueryBySql(string sql)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constant.ElasticSearchConn);
                HttpContent content = new StringContent(JsonHelper.Json(new { sql }), Encoding.UTF8, "application/json");
                HttpResponseMessage msg = client.PostAsync("_sql", content).Result;
                string result = msg.Content.ReadAsStringAsync().Result;
                if (msg.StatusCode != HttpStatusCode.OK)
                {
                    return new List<ESModel<T>>();
                }
                dynamic response = JsonHelper.DeserializeJson<dynamic>(result);
                List<dynamic> hits = JsonHelper.DeserializeJson<List<dynamic>>(response.hits.hits.ToString());
                return hits.Select(o =>
                     new ESModel<T>()
                     {
                         UniqueId = o._id.ToString(),
                         Data = JsonHelper.DeserializeJson<T>(o._source.ToString())
                     }
                ).ToList();
            }
        }
        /// <summary>
        /// 删除 by Sql
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static bool DeleteBySql(string sql)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constant.ElasticSearchConn);
                HttpContent content = new StringContent(JsonHelper.Json(new { sql }), Encoding.UTF8, "application/json");
                HttpResponseMessage msg = client.PostAsync("_sql", content).Result;
                string result = msg.Content.ReadAsStringAsync().Result;
                if (msg.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }
                dynamic response = JsonHelper.DeserializeJson<dynamic>(result);
                return Convert.ToInt32(response.deleted) > 0;
            }
        }
        public static int GetTotalBySql(string sql)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(Constant.ElasticSearchConn);
                HttpContent content = new StringContent(JsonHelper.Json(new { sql }), Encoding.UTF8, "application/json");
                HttpResponseMessage msg = client.PostAsync("_sql", content).Result;
                string result = msg.Content.ReadAsStringAsync().Result;
                if (msg.StatusCode != HttpStatusCode.OK)
                {
                    return 0;
                }
                dynamic response = JsonHelper.DeserializeJson<dynamic>(result);
                return Convert.ToInt32(response.hits.total);
            }
        }
        #endregion

        #region Util-- Common 
        /// <summary>
        /// 构建查询条件
        /// </summary>
        /// <param name="fields"></param>
        /// <returns>{item1:must,item2:should}</returns>
        public static ESConditionModel BuildQueryContainer(List<ESField> fields)
        {
            List<QueryContainer> must = new List<QueryContainer>(),
                should = new List<QueryContainer>(),
                mustNot = new List<QueryContainer>();
            ;
            if (fields != null && fields.Count > 0)
            {

                foreach (var item in fields)
                {
                    QueryContainer container = null;
                    switch (item.QueryType)
                    {
                        case ESQueryType.Match:
                            container = new MatchQuery()
                            {
                                Field = item.Key,
                                Query = item.Value.Item1.ToString(),


                            };
                            break;
                        case ESQueryType.All:
                            container = new MatchAllQuery()
                            {
                            };
                            break;
                        case ESQueryType.Term:
                            container = new TermQuery()
                            {
                                Field = item.Key,
                                Value = item.Value.Item1
                            };
                            break;
                        case ESQueryType.Range:
                            switch (item.DataType)
                            {
                                case ESDataType.Text:
                                    break;
                                case ESDataType.Number:
                                    container = new NumericRangeQuery()
                                    {
                                        Field = item.Key,
                                        GreaterThanOrEqualTo = Convert.ToDouble(item.Value.Item1),
                                        LessThanOrEqualTo = Convert.ToDouble(item.Value.Item2),

                                    };
                                    break;
                                case ESDataType.Date:
                                    container = new DateRangeQuery()
                                    {
                                        Field = item.Key,
                                        GreaterThanOrEqualTo = DateMath.FromString(item.Value.Item1.ToString()),
                                        LessThanOrEqualTo = DateMath.FromString(item.Value.Item2.ToString())
                                    };
                                    break;

                                default:
                                    break;
                            }
                            break;
                    }
                    if (container != null)
                    {
                        switch (item.ConditionType)
                        {
                            case ESConditionType.Must:
                                must.Add(container);
                                break;
                            case ESConditionType.Should:
                                should.Add(container);
                                break;
                            case ESConditionType.MustNot:
                                mustNot.Add(container);
                                break;
                            default:
                                break;
                        }
                    }
                }


            }

            return new ESConditionModel()
            {
                Must = must,
                Should = should,
                MustNot = mustNot

            };
        }
        /// <summary>
        /// 构建高亮配置
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Highlight BuildHightLight(List<string> fields, ESHighLightConfig config)
        {
            var hightLight = new Highlight()
            {
                PreTags = config.PreTags,
                PostTags = config.PostTags,
                Fields = new Dictionary<Field, IHighlightField>(),


            };
            foreach (var item in fields)
            {
                //塞入高亮字段配置（序判断键是否重复）
                if (hightLight.Fields.ContainsKey(item))
                    continue;
                hightLight.Fields.Add(item, new HighlightField()
                {
                    Type = HighlighterType.Plain,
                    ForceSource = true,
                    FragmentSize = 150,
                    Fragmenter = HighlighterFragmenter.Span,
                    NumberOfFragments = 3,
                    NoMatchSize = 150,
                    RequireFieldMatch = true

                });

            }
            return hightLight;
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
            var response = _es.CreateIndex(descriptor);

            if (!response.IsValid)
                throw new Exception("新增Index:" + response.OriginalException.Message);
        }
        public static string BuildHightLightString(string parentStr, string childStr, string preTag, string postTag)
        {
            int equalsCount = 0;
            string result = parentStr;
            if (parentStr.Length < childStr.Length)
            {
                parentStr = childStr;
                childStr = result;
            }
            for (int i = 0; i < childStr.Length; i++)
            {
                if (parentStr[i] != childStr[i])
                {
                    break;
                }
                equalsCount++;
            }
            if (equalsCount > 0)
            {
                result = result.Insert(0, preTag).Insert(equalsCount + preTag.Length, postTag);
            }
            return result;
        }
        #endregion

        #region Util-- Index CURD
        /// <summary>
        /// 索引是否存在
        /// </summary>
        /// <param name="indexName"></param>
        /// <returns></returns>
        public static bool IndexExist(string indexName)
        {
            IndexExistsRequest request = new IndexExistsRequest(indexName);
            var response = _es.IndexExists(request);
            return response.Exists;
        }

        #endregion

        #endregion
    }
}
