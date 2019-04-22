/*****************************************************************************
 * Copyright (c) 2018 掌上互动 All Rights Reserved.
 * CLR版本： 4.0.30319.42000
 * 机器名称：DESKTOP-TTI129R
 * 命名空间：JiuLiao.Common.Elasticsearch.Net.Entity
 * 文件名：  ESIndexMap
 * 版本号：  V1.0.0.0
 * 唯一标识：473322c5-04ec-435c-8c74-ebc747d8cfff
 * 创建人：  MaChong
 * 创建时间：2019/3/6 16:15:29
 * 描述：
 *
 *=====================================================================
 * 修改标记
 * 修改时间：
 * 修改人： 
 * 版本号： 
 * 描述：
 *
/*******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace JiuLiao.Common.Elasticsearch.Net.Entity
{
    public static class ESIndexMap
    {
        /// <summary>
        ///     InitializeIndexMap
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="client"></param>
        /// <param name="index"></param>
        public static void InitializeIndexMap<T>(this IElasticClient client, string index) where T : class
        {
            var descriptor = new CreateIndexDescriptor(index)
                .Mappings(ms => ms
                    .Map<T>(m => m.AutoMap())
                )
                .Settings(s => s.NumberOfShards(5)
                .NumberOfReplicas(1));
            var response = client.CreateIndex(descriptor);

            if (!response.IsValid)
                throw new Exception("新增Index:" + response.OriginalException.Message);
        }
    }
}