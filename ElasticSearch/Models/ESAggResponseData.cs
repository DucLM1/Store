using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearch.Models
{
    public class ESAggResponseData
    {
        public class AggResponseData
        {
            public int took { set; get; }
            public string timed_out { set; get; }
            public Shards _shards { set; get; }
            public Hits hits { set; get; }
            public Aggregation aggregations { set; get; }
        }

        public class Aggregation
        {
            public GroupBucket group_by_city { get; set; }
            public GroupBucket group_by_region { get; set; }
            public GroupBucket group_by_version { get; set; }
            public GroupBucket group_by_model { get; set; }
            public GroupBucket group_by_secondhand { get; set; }
            public GroupBucket group_by_transmissionid { get; set; }
            public GroupBucket group_by_color { get; set; }
            public GroupBucket group_by_type { get; set; }
            public GroupBucket group_by_maker { get; set; }
        }

        public class Hits
        {
            public int total { set; get; }
            public string max_score { set; get; }
            public List<Bucket> hits { set; get; }
        }

        public class Shards
        {
            public string total { set; get; }
            public string successful { set; get; }
            public string failed { set; get; }
        }

        public class Bucket
        {
            public int key { set; get; }
            public double doc_count { set; get; }
        }

        public class GroupBucket
        {
            public int sum_other_doc_count { get; set; }
            public int doc_count_error_upper_bound { get; set; }
            public List<Bucket> buckets { get; set; }
        }
    }
}
