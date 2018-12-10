using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearch.Models
{
    public class ElasticSearchResponseData
    {
        public class HitData
        {
            public string _index { set; get; }
            public string _type { set; get; }
            public string _id { set; get; }
            public string _score { set; get; }
            public ProductInfoOnList _source { set; get; }
            //public long[] sort { get; set; }
        }

        public class Hits
        {
            public int total { set; get; }
            public string max_score { set; get; }
            public List<HitData> hits { set; get; }
        }

        public class Shards
        {
            public string total { set; get; }
            public string successful { set; get; }
            public string failed { set; get; }
        }

        public class ResponseData
        {
            public ResponseData()
            {

            }

            public int took { set; get; }
            public string timed_out { set; get; }
            public Shards _shards { set; get; }
            public Hits hits { set; get; }
        }
    }
}
