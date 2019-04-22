# es-helper
**基于NEST封装**
>【支持SQl查询/删除】
1.环境需求：需要安装elasticsearch-sql插件
2.github下载地址：https://github.com/NLPchina/elasticsearch-sql
3.注意：版本需要与ES版本对应
4.具体操作：
>在ES的plugins目录新建名为sql的文件夹，将下好的elasticsearch-sql插件解压到该目录，重启ES即可
>【接口使用注意事项】
1.es搜索结果默认是按照_score排序（即：匹配程度高低）,如果指定排序字段,_score值为null。因此，如果想将匹配程度高的在前面显示不建议
指定排序字段


