using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearch
{
    public class KeywordBo : IKeywordBo
    {
        private IElasticClient _elasticClient;
        private ElasticSearchConfig _elsElasticSearchConfig;
        private string _elasticsearchUrl = "";
        private ICached _cacheClient;
        private int _cacheType;

        private static int longDurationCachedInMinute = Webconfig.LongCacheTime;
        private static int mediumDurationCachedInMinute = Webconfig.MediumCacheTime;
        private static int shortDurationCachedInMinute = Webconfig.ShortCacheTime;

        private const string FormatDate = "yyyy-MM-ddTHH:mm:ss";
        private const string ElasticFormatDate = "yyyy-MM-ddTHH:mm:ss";

        private TypeName _typeProductPublish = new TypeName() { Name = "product_publish" };
        private TypeName _typeProductPublishSearch = new TypeName() { Name = "product_publish" };
        private Indices _index = Indices.Index(new IndexName() { Name = "cintamobil" });


        private const string prefixKeyCachedSearchProduct = "ElasticSearch_SearchProduct";
        private const string prefixKeyCachedSearchProductCount = "ElasticSearch_CountProduct";

        private const string prefixKeyCachedGetProductByType = "ElasticSearch_GetProductByType";

        #region Constructors

        public KeywordBo(ICached cacheClient)
        {
            this._elasticsearchUrl = AppSettings.Instance.GetString("ElasticSearchUrl");
            if (string.IsNullOrEmpty(this._elasticsearchUrl))
            {
                this._elasticsearchUrl = "http://user:name@103.234.210.71:10001/";
            }

            InitElasticSearch(new ElasticSearchConfig(this._elasticsearchUrl));
            this._cacheClient = cacheClient;
        }

        public KeywordBo(string elasticSearchUrl, ICached cacheClient)
        {
            this._elasticsearchUrl = elasticSearchUrl;

            InitElasticSearch(new ElasticSearchConfig(this._elasticsearchUrl));
            this._cacheClient = cacheClient;

        }

        #endregion

        public List<KeywordEntityForSale> GetKeywordOnPageForSale(SearchKeywordForSaleInfo searchInfo, int productId, int top, ref string header, short type = 1, short year = 0, short limit = 9)
        {
            throw new NotImplementedException();
        }

        public List<KeywordEntityForSale> GetKeywordOnPageForSaleNoCached(SearchKeywordForSaleInfo searchInfo, int productId, int top, ref string header, short type = 1, short year = 0, short limit = 9)
        {
            throw new NotImplementedException();
        }

        #region private methods

        private KeywordEntityForSale ConvertHitToKeywordIno(IHit<KeywordEntityForSale> hit)
        {
            Func<IHit<KeywordEntityForSale>, KeywordEntityForSale> func = (x) =>
            {
                hit.Source.id = Convert.ToInt32(hit.Id);
                return hit.Source;
            };

            return func.Invoke(hit);
        }

        private void InitElasticSearch(ElasticSearchConfig elsElasticSearchConfig)
        {
            Uri uri = new Uri(elsElasticSearchConfig.Url);
            this._elasticClient = new ElasticClient(uri);
        }

        #endregion
    }
}
