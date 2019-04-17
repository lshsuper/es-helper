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
    /// 说明：
    ///1.es扩展接口类（自定义接口格式）
    ///2.均为单索引操作，主要为简化操作流程，复杂操作需另外实现
    ///3.Sql相关接口需要安装【elasticsearch-sql-x】插件才可以使用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ESContext<T> : ESContextBase<T> where T : class, new()
    {
        #region Context-- Util

        #region Util-- Document CURD

        /// <summary>
        /// 查询列表（by page）
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static ESPageInfo<T> Query(ESSearchOption option)
        {

            ISearchRequest searchRequest = new SearchRequest<T>(Indices.Parse(option.IndexName), Types.Parse(option.TypeName));
            var data = new ESPageInfo<T>();
            var queryCondations = BuildQueryContainer(option.Where, option.Analyzer);
            var shouldQuerys = queryCondations.Should;
            var mustQuerys = queryCondations.Must;
            var mustNot = queryCondations.MustNot;

            #region +多条件模糊匹配
            searchRequest.Query = new BoolQuery()
            {
                Should = shouldQuerys,
                Must = mustQuerys,
                MustNot = mustNot,

            };

            if (option.ESHighLightConfig != null)
            {
                searchRequest.Highlight = BuildHightLight(option.Where.Select(o => o.Key).ToList(), option.ESHighLightConfig);
            }
            #endregion

            #region +多字段排序
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

            #region +组装结果集
            searchRequest.From = (option.PageIndex - 1) * option.PageSize;
            searchRequest.Size = option.PageSize;
            var result = GetInstance.Search<T>(searchRequest);
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
                             Data = o.Source,
                             Score = o.Score,
                             IndexName = o.Index,
                             TypeName = o.Type
                         }).ToList();
            }
            else
            {
                IReadOnlyCollection<IHit<T>> hits = result.Hits;  //找到高亮列表
                foreach (var hit in hits)
                {
                    var currentHightList = hit.Highlights;
                    if (hit.Source is Dictionary<string, object>)
                    {
                        Dictionary<string, object> currentSource = hit.Source as Dictionary<string, object>;
                        List<string> keys = currentSource.Keys.ToList();
                        foreach (var key in keys)
                        {
                            KeyValuePair<string, HighlightHit> currentMatch = currentHightList.FirstOrDefault(o => o.Key.ToLower()== key.ToLower());
                            if (currentMatch.Key != null)
                            {
                                
                                currentSource[key] = currentMatch.Value.Highlights.First();
                            }
                        }
                    }
                    else
                    {
                        var sourceProps = hit.Source.GetType().GetProperties();
                        foreach (var sourceProp in sourceProps)
                        {
                            KeyValuePair<string, HighlightHit> currentMatch = currentHightList.FirstOrDefault(o => o.Key == sourceProp.Name);
                            if (currentMatch.Key != null)
                            {
                                sourceProp.SetValue(hit.Source, Convert.ChangeType(currentMatch.Value.Highlights.First(), sourceProp.PropertyType));
                            }
                        }
                    }
                    data.DataSource.Add(new ESModel<T>()
                    {
                        Data = hit.Source,
                        UniqueId = hit.Id,
                        Score = hit.Score,
                        IndexName = hit.Index,
                        TypeName = hit.Type
                    });
                }
            }
            #endregion

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
            var result = GetInstance.Search<T>(request);
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
            var response = GetInstance.Update(request);
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
            var response = GetInstance.Update(request);
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


            var queryCondations = BuildQueryContainer(option.Where, option.Analyzer);
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

            var res = GetInstance.UpdateByQuery(request);
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
            var response = GetInstance.Get<T>(request);
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
            var result = GetInstance.Search<T>(s => s.Index(indexName).Type(typeName)
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
            var queryCondations = BuildQueryContainer(option.Where, option.Analyzer);
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

            var res = GetInstance.DeleteByQuery(request);
            return res.Deleted > 0;
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
        public static ESConditionModel BuildQueryContainer(List<ESField> fields, string analyzer)
        {
            List<QueryContainer> must = new List<QueryContainer>(),
                should = new List<QueryContainer>(),
                mustNot = new List<QueryContainer>();
            if (fields != null && fields.Count > 0)
            {
                foreach (var item in fields)
                {
                    QueryContainer container = null;
                    //1.数据类型辨析（暂时不做实现）
                    if (item.DataType == ESDataType.GeoPoint)
                    {
                        //switch (item.QueryType)
                        //{
                        //    case ESQueryType.Geo_Distance:
                        //        container = new GeoDistanceQuery()
                        //        {
                        //            Location = new GeoLocation(0.1, 0.2),
                        //            Distance =new Distance(),
                        //        };
                        //        break;
                        //    case ESQueryType.Geo_Polygon:
                        //        container = new GeoPolygonQuery(){
                        //         Points=,
                        //    };
                        //        break;
                        //    case ESQueryType.Geo_Bounding_Box:
                        //        container = new GeoBoundingBoxQuery()
                        //        {
                        //            BoundingBox = new BoundingBox()
                        //            {
                        //                BottomRight =,
                        //                TopLeft =,
                        //            }
                        //        };
                        //        break;
                        //}
                    }
                    else
                    {
                        switch (item.QueryType)
                        {
                            case ESQueryType.Match:
                                container = new MatchQuery()
                                {
                                    Analyzer = analyzer,
                                    Field = item.Key,
                                    Query = item.Value.Item1.ToString(),
                                };
                                break;
                            case ESQueryType.All:
                                container = new MatchAllQuery()
                                {

                                };
                                break;
                            case ESQueryType.Match_Phrase:
                                container = new MatchPhraseQuery()
                                {
                                    Field = item.Key,
                                    Analyzer = analyzer,
                                    Query = item.Value.Item1.ToString()
                                };
                                break;
                            case ESQueryType.Match_Phrase_Prefix:
                                container = new MatchPhrasePrefixQuery()
                                {
                                    Field = item.Key,
                                    Analyzer = analyzer,
                                    Query = item.Value.ToString()
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
                    }
                    //2.条件类型解析
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
        /// 构建高亮字符串
        /// </summary>
        /// <param name="parentStr"></param>
        /// <param name="childStr"></param>
        /// <param name="preTag"></param>
        /// <param name="postTag"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 构建分组key
        /// </summary>
        /// <param name="groupBys"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private static string BuildGroupKey(HashSet<string> groupBys, string tag)
        {

            HashSet<string> set = new HashSet<string>();
            foreach (var item in groupBys)
            {
                set.Add($"doc[{item}].values");
            }
            return string.Join($"+'{tag}'+", set);
        }
        
        #endregion

        #endregion
    }
}
