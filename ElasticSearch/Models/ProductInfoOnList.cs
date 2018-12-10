using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearch.Models
{
    public class ProductInfoOnList
    {
        public int productid { get; set; }

        public string title { get; set; }

        public int maker { get; set; }

        public int model { get; set; }

        public short version { get; set; }

        public int transmissionid { get; set; }

        public decimal price { get; set; }

        public short year { get; set; }

        public int city { get; set; }

        public int numofkm { get; set; }

        public string numofkmunit { get; set; }

        public string image { get; set; }

        public int usertype { get; set; }

        public int secondhand { get; set; }

        public int createduser { get; set; }

        public long createdate { get; set; }

        public bool ispublish { get; set; }

        public string branchname { get; set; }

        public string modelname { get; set; }

        public string versionname { get; set; }

        public string cityname { get; set; }

        public long publishdate { get; set; }

        public bool color { get; set; }

        public bool type { get; set; }

        public int viptype { get; set; }
    }
}
