using JiuLiao.Common.Elasticsearch.Net.Entity;
using JiuLiao.Model.Model;
using JiuLiao.Model.Model.ElasticSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiuLiao.Common.Elasticsearch.Net
{
    /// <summary>
    ///消息队列版追加doc
    /// </summary>
    public class DocMqProducer
    {
       
        /// <summary>
        /// 塞词段
        /// </summary>
        /// <param name="model"></param>
        public static void PutWordDoc(List<ESWordsDto> model)
        {
            try
            {
                //消息推送
                CommonMessageDto msgItem = new CommonMessageDto()
                {
                    MsgType = Model.Enum.MsgNotificationType.Words,
                    Payload = JsonHelper.Json(model),
                    UserId = 0
                };
                RabbitMqHelper.SendMessage("JiuLiao_CommonMessage", JsonHelper.Json(msgItem));
            }
            catch (Exception ex)
            {

            }

        }
        
    }
}
