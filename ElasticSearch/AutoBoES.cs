using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;

namespace ElasticSearch
{
    public class AutoBoES
    {
        private static ElasticLowLevelClient elasticClient = null;

        private const string FormatDate = "yyyy-MM-ddTHH:mm:ss";
        private const string ElasticFormatDate = "yyyy-MM-ddTHH:mm:ss";

        private string _typeProductPublish = "product_publish";
        private string _index = "cintamobil";


        private const string prefixCacheSearchProduct = "ES_SearchProduct";
        private const string prefixCacheSearchProductByKeyword = "ES_SearchProductByKeyword";
        private const string prefixCacheSearchProductCount = "ES_CountProduct";
        private const string prefixCacheGetProductByType = "ES_GetProductByType";
        private const string prefixCacheGetProductByPrice = "ES_GetProductByPrice";
        private const string prefixCacheCountProductForLeftMenu = "ES_CountProductForLeftMenu";

        public static bool AllowCachedBeforeAccessES = AppSettings.Instance.GetBool("CachedBeforeGetES", true);

        private static ElasticLowLevelClient _elasticClient
        {
            get
            {
                if (elasticClient == null)
                {
                    string _elasticsearchUrl = AppSettings.Instance.GetString("ElasticSearchUrl");
                    if (string.IsNullOrEmpty(_elasticsearchUrl))
                    {
                        _elasticsearchUrl = "http://user:name@2.2.2.1:10001/";
                    }
                    var node = new Uri(_elasticsearchUrl);
                    var connectionPool = new SingleNodeConnectionPool(node);
                    var config = new ConnectionConfiguration(connectionPool).DeadTimeout(TimeSpan.FromSeconds(3));

                    elasticClient = new Elasticsearch.Net.ElasticLowLevelClient(config);
                }

                return elasticClient;
            }
        }

        #region public methods

        public List<ProductInfo> SearchProduct(SearchProductInfo searchInfo, out int totalRecord, out int totalRecordSuggest, int limit = 20, Guid guid = new Guid())
        {
            totalRecord = 0;
            totalRecordSuggest = 0;
            searchInfo.pageSize = limit;

            SearchProductInfoES searchParameters = new SearchProductInfoES(searchInfo);

            List<ProductInfo> productList = new List<Models.ProductInfo>();

            try
            {
                productList = SearchProductNoCached(searchParameters, out totalRecord, limit, guid);

                if (totalRecord <= 0 || productList == null || !productList.Any())
                {
                    SearchProductInfo searchInfoSuggest = (SearchProductInfo)searchInfo.Clone();

                    searchInfoSuggest.pageSize = limit;
                    searchInfoSuggest = _searchProductSuggest(searchInfoSuggest);

                    productList = SearchProductNoCached(searchParameters, out totalRecordSuggest, limit, guid);
                }
            }
            catch (Exception ex)
            {
                Logger<>.ErrorLog(ex);
            }

            return productList;
        }

        public int CountSearchProduct(SearchProductInfo searchInfo)
        {
            try
            {
                return CountSearchProductNoCached(searchInfo);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("CountSearchProduct from ElasticSearch: {0}", ex));
            }
        }

        public List<ProductInfo> GetListAutoSameType(short brandId, short modelId, short cityId, double minPrice, double maxPrice, int ignoreProductId, int top, Guid guid = new Guid())
        {
            try
            {
                List<ProductInfo> productList = GetListAutoSameTypeNoCached(brandId, modelId, cityId, minPrice, maxPrice, ignoreProductId, top, guid);

                return productList;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GetListAutoSameType from ElasticSearch: {0}", ex));
            }
        }

        public List<ProductInfo> GetListAutoSamePrice(short brandId, short cityId, double minPrice, double maxPrice, List<int> ignoreProductIds, int top, Guid guid = new Guid())
        {
            try
            {
                List<ProductInfo> productList = GetListAutoSamePriceNoCached(brandId, cityId, minPrice, maxPrice, ignoreProductIds, top, guid);

                return productList;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GetListAutoSamePrice from ElasticSearch: {0}", ex));
            }
        }

        public List<ProductInfo> GetListAutoByKeyword(string keyword, int haveImage, int pageIndex, int pageSize, int orderBy, out int totalRecord, Guid guid = new Guid())
        {
            totalRecord = 0;

            try
            {
                List<ProductInfo> productList = _getListAutoByKeyword(keyword, haveImage, pageIndex, pageSize, orderBy, out totalRecord, guid);

                return productList;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("GetListAutoSamePrice from ElasticSearch: {0}", ex));
            }
        }

        public Models.ESAggResponseData.Aggregation CountProductForLeftMenu(SearchProductInfo searchInfo)
        {
            SearchProductInfoES searchParameters = new SearchProductInfoES(searchInfo);

            Models.ESAggResponseData.Aggregation aggregation = new Models.ESAggResponseData.Aggregation();

            Guid guid = Guid.NewGuid();

            try
            {
                aggregation = CountProductForLeftMenuNoCached(searchParameters, guid);
            }
            catch (Exception ex)
            {
                Logger<>.ErrorLog(ex);
            }
            return aggregation;
        }

        #endregion

        #region private methods nocached

        private List<ProductInfo> SearchProductNoCached(SearchProductInfoES searchInfo, out int totalRecord, int limit = 20, Guid guid = new Guid())
        {
            totalRecord = 0;
            try
            {
                System.DateTime fromDate = DateTime.Now;

                searchInfo.pageSize = limit;

                List<ProductInfo> productList = new List<Models.ProductInfo>();

                string query = _genQueryStringForSearchProduct(searchInfo);


                //Console.WriteLine(Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1"));

                ElasticsearchResponse<Models.ElasticSearchResponseData.ResponseData> response = _elasticClient.Search<Models.ElasticSearchResponseData.ResponseData>(_index, _typeProductPublish, query);

                double eslapeTimeSearch = (DateTime.Now - fromDate).TotalMilliseconds;

                if (response != null && response.Success)
                {
                    Models.ElasticSearchResponseData.ResponseData responseData = response.Body;

                    var hit = responseData.hits.hits;

                    if (hit != null && hit.Any())
                    {
                        productList = hit.Select(x => new ProductInfo(x._source)).ToList();
                    }

                    totalRecord = responseData.hits.total;
                }

                System.DateTime endDate = DateTime.Now;
                double totalMilliseconds = (endDate - fromDate).TotalMilliseconds;

                if (totalMilliseconds > 1000)
                {
                    Logger<>.WriteLog(Logger<>.LogType.Trace, string.Format("SearchProductNoCached - {4} - timeleft: {0:n0}ms, searchES: {1}, took: {2} with query: {3}", totalMilliseconds, eslapeTimeSearch, response.Body.took, Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1"), guid));
                }

                return productList;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("SearchProduct from ElasticSearch: {0}", ex));
            }
        }

        private int CountSearchProductNoCached(SearchProductInfo searchInfo)
        {
            System.DateTime fromDate = DateTime.Now;
            int totalRecord = 0;

            SearchProductInfoES searchParameters = new SearchProductInfoES(searchInfo);

            string query = _genQueryStringForSearchProduct(searchParameters);

            ElasticsearchResponse<Models.ElasticSearchResponseData.ResponseData> response = _elasticClient.Count<Models.ElasticSearchResponseData.ResponseData>(_index, _typeProductPublish, query);
            if (response != null && response.Success)
            {
                totalRecord = response.AuditTrail.Count();
            }

            System.DateTime endDate = DateTime.Now;
            double totalMilliseconds = (endDate - fromDate).TotalMilliseconds;

            if (totalMilliseconds > 1000)
            {
                Logger<>.WriteLog(Logger<>.LogType.Trace, string.Format("CountSearchProductNoCached timeleft: {0:n0}ms, took: {1} width query: {2}", totalMilliseconds, response.Body.took, Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1")));
            }

            return totalRecord;
        }

        private List<ProductInfo> GetListAutoSameTypeNoCached(short brandId, short modelId, short cityId, double minPrice, double maxPrice, int ignoreProductId, int top, Guid guid = new Guid())
        {
            List<ProductInfo> productList = new List<Models.ProductInfo>();

            long dateNow = DateTime.Now.Ticks;
            int totalRecord = 0;
            int userType = (int)UserType.NormalUser;
            int status = 2; // Tin đã duyệt

            List<int> ignoreProductIds = new List<int>();

            if (ignoreProductId > 0)
            {
                ignoreProductIds.Add(ignoreProductId);
            }

            //int defTop = ignoreProductIds != null && ignoreProductIds.Any() ? ignoreProductIds.Count() + top : top;
            int defTop = top;

            // Query theo full điều kiện
            productList = _queryProductSameType(dateNow, brandId, modelId, cityId, minPrice, maxPrice, userType, status, defTop, ignoreProductIds, out totalRecord, guid);

            #region Query product, Nếu chưa đủ số bản ghi yêu cầu (top) thì giảm điều kiện (từ phải sang trái)

            // Nếu chưa đủ số bản ghi yêu cầu (top) thì giảm điều kiện (từ phải sang trái)
            if (totalRecord < defTop)
            {
                defTop = defTop - totalRecord;
                userType = 0;

                if (totalRecord > 0)
                {
                    ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                }

                productList = _queryProductSameType(dateNow, brandId, modelId, cityId, minPrice, maxPrice, userType, status, defTop, ignoreProductIds, out totalRecord, guid);

                if (totalRecord < defTop)
                {
                    defTop = defTop - totalRecord;
                    userType = 0;
                    minPrice = 0;
                    maxPrice = 0;

                    if (totalRecord > 0)
                    {
                        ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                    }

                    productList.AddRange(_queryProductSameType(dateNow, brandId, modelId, cityId, minPrice, maxPrice, userType, status, defTop, ignoreProductIds, out totalRecord, guid));

                    if (totalRecord < defTop)
                    {
                        defTop = defTop - totalRecord;
                        userType = 0;
                        minPrice = 0;
                        maxPrice = 0;
                        status = 0;

                        if (totalRecord > 0)
                        {
                            ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                        }

                        productList.AddRange(_queryProductSameType(dateNow, brandId, modelId, cityId, minPrice, maxPrice, userType, status, defTop, ignoreProductIds, out totalRecord, guid));

                        if (totalRecord < defTop)
                        {
                            defTop = defTop - totalRecord;
                            userType = 0;
                            minPrice = 0;
                            maxPrice = 0;
                            cityId = 0;
                            status = 0;

                            if (totalRecord > 0)
                            {
                                ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                            }

                            productList.AddRange(_queryProductSameType(dateNow, brandId, modelId, cityId, minPrice, maxPrice, userType, status, defTop, ignoreProductIds, out totalRecord, guid));

                            if (totalRecord < defTop)
                            {
                                defTop = defTop - totalRecord;
                                userType = 0;
                                minPrice = 0;
                                maxPrice = 0;
                                cityId = 0;
                                modelId = 0;

                                if (totalRecord > 0)
                                {
                                    ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                                }

                                productList.AddRange(_queryProductSameType(dateNow, brandId, modelId, cityId, minPrice, maxPrice, userType, status, defTop, ignoreProductIds, out totalRecord, guid));

                                if (totalRecord < defTop)
                                {
                                    defTop = defTop - totalRecord;
                                    userType = 0;
                                    minPrice = 0;
                                    maxPrice = 0;
                                    cityId = 0;
                                    modelId = 0;
                                    brandId = 0;

                                    if (totalRecord > 0)
                                    {
                                        ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                                    }

                                    productList.AddRange(_queryProductSameType(dateNow, brandId, modelId, cityId, minPrice, maxPrice, userType, status, defTop, ignoreProductIds, out totalRecord, guid));
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            if (ignoreProductIds != null && ignoreProductIds.Any())
            {
                productList = productList.Where(x => ignoreProductIds.Any(y => y != x.Productid)).Take(top).ToList();
            }

            return productList;
        }

        private List<ProductInfo> GetListAutoSamePriceNoCached(short brandId, short cityId, double minPrice, double maxPrice, List<int> listIgnoreProductId, int top, Guid guid = new Guid())
        {
            List<ProductInfo> productList = new List<Models.ProductInfo>();

            long dateNow = DateTime.Now.Ticks;
            int totalRecord = 0;
            int userType = (int)UserType.NormalUser;
            int status = 2;// Tin đã duyệt

            List<int> ignoreProductIds = new List<int>();

            if (listIgnoreProductId != null && listIgnoreProductId.Any())
            {
                ignoreProductIds.AddRange(listIgnoreProductId);
            }

            //int defTop = ignoreProductIds != null && ignoreProductIds.Any() ? top + ignoreProductIds.Count() : top;
            int defTop = top;

            // Query theo full điều kiện
            productList = _queryProductSamePrice(dateNow, brandId, cityId, minPrice, maxPrice, userType, status, defTop, listIgnoreProductId, out totalRecord, guid);

            #region Query product, Nếu chưa đủ số bản ghi yêu cầu (top) thì giảm điều kiện (từ phải sang trái)

            // Nếu chưa đủ số bản ghi yêu cầu (top) thì giảm điều kiện (từ phải sang trái)
            if (totalRecord < defTop)
            {
                defTop = defTop - totalRecord;
                userType = 0;

                if (totalRecord > 0)
                {
                    ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                }

                productList = _queryProductSamePrice(dateNow, brandId, cityId, minPrice, maxPrice, userType, status, defTop, listIgnoreProductId, out totalRecord, guid);

                if (totalRecord < defTop)
                {
                    defTop = defTop - totalRecord;
                    userType = 0;
                    status = 0;

                    if (totalRecord > 0)
                    {
                        ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                    }

                    productList.AddRange(_queryProductSamePrice(dateNow, brandId, cityId, minPrice, maxPrice, userType, status, defTop, listIgnoreProductId, out totalRecord, guid));

                    if (totalRecord < defTop)
                    {
                        defTop = defTop - totalRecord;
                        userType = 0;
                        brandId = 0;
                        status = 0;

                        if (totalRecord > 0)
                        {
                            ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                        }

                        productList.AddRange(_queryProductSamePrice(dateNow, brandId, cityId, minPrice, maxPrice, userType, status, defTop, listIgnoreProductId, out totalRecord, guid));

                        if (totalRecord < defTop)
                        {
                            defTop = defTop - totalRecord;
                            userType = 0;
                            brandId = 0;
                            cityId = 0;
                            status = 0;

                            if (totalRecord > 0)
                            {
                                ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                            }

                            productList.AddRange(_queryProductSamePrice(dateNow, brandId, cityId, minPrice, maxPrice, userType, status, defTop, listIgnoreProductId, out totalRecord, guid));

                            if (totalRecord < defTop)
                            {
                                defTop = defTop - totalRecord;
                                userType = 0;
                                brandId = 0;
                                cityId = 0;
                                status = 0;
                                minPrice = 0;
                                maxPrice = 0;

                                if (totalRecord > 0)
                                {
                                    ignoreProductIds.AddRange(productList.Select(s => s.Productid).ToList());
                                }

                                productList.AddRange(_queryProductSamePrice(dateNow, brandId, cityId, minPrice, maxPrice, userType, status, defTop, listIgnoreProductId, out totalRecord, guid));
                            }
                        }
                    }
                }
            }

            #endregion

            if (productList != null && productList.Any())
            {
                productList = productList.Where(x => ignoreProductIds.Any(y => y != x.Productid)).Take(top).ToList();
            }

            return productList;
        }

        private Models.ESAggResponseData.Aggregation CountProductForLeftMenuNoCached(SearchProductInfoES searchInfo, Guid guid)
        {
            Models.ESAggResponseData.Aggregation aggregate = new Models.ESAggResponseData.Aggregation();

            System.DateTime fromDate = DateTime.Now;

            string query = _genQueryStringForCountProductLeftMenu(searchInfo);

            ElasticsearchResponse<Models.ESAggResponseData.AggResponseData> response = _elasticClient.Search<Models.ESAggResponseData.AggResponseData>(_index, _typeProductPublish, query);

            if (response != null && response.Success)
            {
                Models.ESAggResponseData.AggResponseData responseData = response.Body;

                if (responseData != null)
                {
                    aggregate = responseData.aggregations;
                }
            }


            System.DateTime endDate = DateTime.Now;
            double totalMilliseconds = (endDate - fromDate).TotalMilliseconds;

            if (totalMilliseconds > 1000)
            {
                Logger<>.WriteLog(Logger<>.LogType.Trace, string.Format("CountProductForLeftMenuNoCached timeleft: {0:n0}ms, guid: {1}, query: {2}", totalMilliseconds, guid, Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1")));
            }

            return aggregate;
        }

        #endregion

        #region private methods

        private List<ProductInfo> _queryProductSameType(long dateNow, short brandId, short modelId, short cityId, double minPrice, double maxPrice, int userType, int status, int top, List<int> ignoreProductList, out int totalRecord, Guid guid = new Guid())
        {
            totalRecord = 0;
            System.DateTime fromDate = DateTime.Now;

            List<ProductInfo> productList = new List<ProductInfo>();

            #region build Query String


            string query = string.Empty;

            StringBuilder strBuilder = new StringBuilder();

            string jsonSort = buildSortOrder("publishdate", "desc");

            #region begin query and include fields

            strBuilder.AppendFormat(@"
                {{
                  ""from"": {0},
                  ""size"": {1},
                  ""sort"": [
                        {2}
                  ],
                  ""_source"": {{
                    ""includes"": [
                      ""productid"",
                      ""title"",
                      ""maker"",
                      ""model"",
                      ""version"",
                      ""transmissionid"",
                      ""price"",
                      ""year"",
                      ""city"",
                      ""numofkm"",
                      ""numofkmunit"",
                      ""image"",
                      ""usertype"",
                      ""secondhand"",
                      ""createduser"",
                      ""createdate"",
                      ""ispublish"",
                      ""branchname"",
                      ""modelname"",
                      ""versionname"",
                      ""cityname"",
                      ""publishdate"",
                      ""color"",
                      ""type""
                    ]
                  }}", 0, top, jsonSort);

            #endregion

            strBuilder.Append(@",");
            // Begin Query
            strBuilder.Append(@"
                ""query"": {
                    ""bool"": 
                     {
                ");

            #region build query details by conditions must

            strBuilder.Append(@"""must"": [");

            // Là tin published: ispublish = true
            strBuilder.Append(buildTermQuery("ispublish", true));
            // Phải có ảnh: image != null | empty
            strBuilder.Append(",");
            strBuilder.Append(buildExistsQuery("image"));
            // Phải có giá: price > 0
            strBuilder.Append(",");
            strBuilder.Append(buildRangeQuery("price", 0, Operator.GreaterThan));

            // Phải còn hạn 
            if (dateNow > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("startdate", dateNow, Operator.LessThanOrEquals));
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("enddate", dateNow, Operator.GreaterThanOrEquals));
            }

            // Có thể cùng Brand
            if (brandId > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("maker", brandId));
            }

            // Có thể cùng model
            if (modelId > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("model", modelId));
            }

            // Có thể cùng thành phố
            if (cityId > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("city", cityId));
            }

            // Có thể tin đã được duyệt
            if (status > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("status", status));
            }

            // Có thể >= -10% price và <= +10% price
            if (minPrice > 0 && maxPrice > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("price", minPrice, Operator.GreaterThanOrEquals));
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("price", maxPrice, Operator.LessThanOrEquals));
            }

            // Ưu tiên tin của user tự đăng: userType = (int)UserType.NormalUser
            if (userType == (int)UserType.NormalUser)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("usertype", userType));
            }

            strBuilder.Append("]");

            #endregion

            #region build query details by conditions must not

            if (ignoreProductList != null && ignoreProductList.Any())
            {
                strBuilder.Append(",");
                strBuilder.Append(@"""must_not"": [");

                bool isStartAppended = false;
                foreach (int productid in ignoreProductList)
                {
                    if (isStartAppended) strBuilder.Append(",");

                    strBuilder.Append(buildTermQuery("productid", productid));

                    isStartAppended = true;
                }
                strBuilder.Append("]");
            }

            #endregion

            // End bool
            strBuilder.Append(@"
                         }
                ");
            // End query string
            strBuilder.Append(@"
                    }
                ");

            // End Query
            strBuilder.Append(@"
                }");

            if (strBuilder != null)
            {
                query = strBuilder.ToString();

                //output = Regex.Replace(output, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
            }

            #endregion

            //Console.WriteLine(Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1"));

            ElasticsearchResponse<Models.ElasticSearchResponseData.ResponseData> response = _elasticClient.Search<Models.ElasticSearchResponseData.ResponseData>(_index, _typeProductPublish, query);

            if (response != null && response.Success)
            {
                Models.ElasticSearchResponseData.ResponseData responseData = response.Body;

                var hit = responseData.hits.hits;

                if (hit != null && hit.Any())
                {
                    productList = hit.Select(x => new ProductInfo(x._source)).ToList();
                }
            }

            System.DateTime endDate = DateTime.Now;
            double totalMilliseconds = (endDate - fromDate).TotalMilliseconds;

            if (totalMilliseconds > 1000)
            {
                Logger<>.WriteLog(Logger<>.LogType.Trace, string.Format("_queryProductSameType - {3} - timeleft: {0:n0}ms, took: {1} width query: {2}", totalMilliseconds, response.Body.took, Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1"), guid));
            }

            return productList;
        }

        private List<ProductInfo> _queryProductSamePrice(long dateNow, short brandId, short cityId, double minPrice, double maxPrice, int userType, int status, int top, List<int> ignoreProductList, out int totalRecord, Guid guid = new Guid())
        {
            totalRecord = 0;
            System.DateTime fromDate = DateTime.Now;

            List<ProductInfo> productList = new List<ProductInfo>();

            #region build Query String


            string query = string.Empty;

            StringBuilder strBuilder = new StringBuilder();

            string jsonSort = buildSortOrder("publishdate", "desc");

            #region begin query and include fields

            strBuilder.AppendFormat(@"
                {{
                  ""from"": {0},
                  ""size"": {1},
                  ""sort"": [
                        {2}
                  ],
                  ""_source"": {{
                    ""includes"": [
                      ""productid"",
                      ""title"",
                      ""maker"",
                      ""model"",
                      ""version"",
                      ""transmissionid"",
                      ""price"",
                      ""year"",
                      ""city"",
                      ""numofkm"",
                      ""numofkmunit"",
                      ""image"",
                      ""usertype"",
                      ""secondhand"",
                      ""createduser"",
                      ""createdate"",
                      ""ispublish"",
                      ""branchname"",
                      ""modelname"",
                      ""versionname"",
                      ""cityname"",
                      ""publishdate"",
                      ""color"",
                      ""type""
                    ]
                  }}", 0, top, jsonSort);

            #endregion

            strBuilder.Append(@",");
            // Begin Query
            strBuilder.Append(@"
                ""query"": {
                    ""bool"": 
                     {
                ");

            #region build query details by conditions must

            strBuilder.Append(@"""must"": [");

            // Là tin published: ispublish = true
            strBuilder.Append(buildTermQuery("ispublish", true));
            // Phải có ảnh: image != null | empty
            strBuilder.Append(",");
            strBuilder.Append(buildExistsQuery("image"));
            // Phải có giá: price > 0
            strBuilder.Append(",");
            strBuilder.Append(buildRangeQuery("price", 0, Operator.GreaterThan));

            // Phải còn hạn 
            if (dateNow > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("startdate", dateNow, Operator.LessThanOrEquals));
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("enddate", dateNow, Operator.GreaterThanOrEquals));
            }

            // Có thể >= -10% price và <= +10% price
            if (minPrice > 0 && maxPrice > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("price", minPrice, Operator.GreaterThanOrEquals));
                strBuilder.Append(",");
                strBuilder.Append(buildRangeQuery("price", maxPrice, Operator.LessThanOrEquals));
            }

            // Có thể cùng thành phố
            if (cityId > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("city", cityId));
            }

            // Có thể tin đã được duyệt
            if (status > 0)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("status", status));
            }

            // Ưu tiên tin của user tự đăng: userType = (int)UserType.NormalUser
            if (userType == (int)UserType.NormalUser)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildTermQuery("usertype", userType));
            }

            strBuilder.Append("]");

            #endregion

            #region  build query details by conditions must not

            // Có thể khác Brand
            if (brandId > 0 || ignoreProductList != null || ignoreProductList.Any())
            {
                strBuilder.Append(",");
                strBuilder.Append(@"""must_not"": [");

                if (brandId > 0)
                {
                    strBuilder.Append(buildTermQuery("maker", brandId));
                }

                if (ignoreProductList != null && ignoreProductList.Any())
                {
                    bool isStartAppended = false;

                    if (brandId > 0)
                    {
                        isStartAppended = true;
                    }

                    foreach (int productid in ignoreProductList)
                    {
                        if (isStartAppended) strBuilder.Append(",");

                        strBuilder.Append(buildTermQuery("productid", productid));

                        isStartAppended = true;
                    }
                }
                strBuilder.Append("]");
            }

            #endregion

            // End bool
            strBuilder.Append(@"
                         }
                ");
            // End query string
            strBuilder.Append(@"
                    }
                ");

            // End Query
            strBuilder.Append(@"
                }");

            if (strBuilder != null)
            {
                query = strBuilder.ToString();

                //output = Regex.Replace(output, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
            }

            #endregion

            //Console.WriteLine(Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1"));

            ElasticsearchResponse<Models.ElasticSearchResponseData.ResponseData> response = _elasticClient.Search<Models.ElasticSearchResponseData.ResponseData>(_index, _typeProductPublish, query);

            if (response != null && response.Success)
            {
                Models.ElasticSearchResponseData.ResponseData responseData = response.Body;

                var hit = responseData.hits.hits;

                if (hit != null && hit.Any())
                {
                    productList = hit.Select(x => new ProductInfo(x._source)).ToList();
                }
            }

            System.DateTime endDate = DateTime.Now;
            double totalMilliseconds = (endDate - fromDate).TotalMilliseconds;

            if (totalMilliseconds > 1000)
            {
                Logger<>.WriteLog(Logger<>.LogType.Trace, string.Format("_queryProductSamePrice - {3} - timeleft: {0:n0}ms, took: {1} width query: {2}", totalMilliseconds, response.Body.took, Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1"), guid));
            }

            return productList;
        }

        private List<ProductInfo> _getListAutoByKeyword(string keyword, int haveImage, int pageIndex, int pageSize, int orderBy, out int totalRecord, Guid guid = new Guid())
        {
            totalRecord = 0;
            System.DateTime fromDate = DateTime.Now;

            if (pageIndex <= 0) pageIndex = 1;

            List<ProductInfo> productList = new List<ProductInfo>();

            #region build Query String


            string query = string.Empty;

            StringBuilder strBuilder = new StringBuilder();

            #region sort

            Dictionary<string, string> dictSort = new Dictionary<string, string>();

            string jsonSort = string.Empty;

            switch (orderBy)
            {
                case (int)ArrangeId.PublishDate:
                    dictSort.Add("haveprice", "asc");
                    break;
                case (int)ArrangeId.PriceASC:
                    dictSort.Add("haveprice", "desc");
                    dictSort.Add("price", "asc");
                    break;
                case (int)ArrangeId.PriceDESC:
                    dictSort.Add("haveprice", "desc");
                    dictSort.Add("price", "desc");
                    break;
                case (int)ArrangeId.YearOldToNew:
                    dictSort.Add("year", "asc");
                    break;
                case (int)ArrangeId.YearNewToOld:
                    dictSort.Add("year", "desc");
                    break;
                case (int)ArrangeId.NumOfKmASC:
                    dictSort.Add("numofkm", "asc");
                    break;
                case (int)ArrangeId.NumOfKmDESC:
                    dictSort.Add("numofkm", "desc");
                    break;
                case (int)ArrangeId.CreateDateASC:
                    dictSort.Add("publishdate", "asc");
                    break;
                case (int)ArrangeId.CreateDateDESC:
                default:
                    // dictSort.Add("publishdate", "desc");
                    dictSort.Add("viptype", "desc");
                    dictSort.Add("publishdate", "desc");
                    break;
            }
            jsonSort = buildSortOrder(dictSort);

            if (!string.IsNullOrEmpty(jsonSort)) jsonSort = jsonSort + ",";

            #endregion

            #region begin query and include fields

            strBuilder.AppendFormat(@"
                {{
                  ""from"": {0},
                  ""size"": {1},
                  {2}
                  ""_source"": {{
                    ""includes"": [
                      ""productid"",
                      ""title"",
                      ""maker"",
                      ""model"",
                      ""version"",
                      ""transmissionid"",
                      ""price"",
                      ""year"",
                      ""city"",
                      ""numofkm"",
                      ""numofkmunit"",
                      ""image"",
                      ""usertype"",
                      ""secondhand"",
                      ""createduser"",
                      ""createdate"",
                      ""ispublish"",
                      ""branchname"",
                      ""modelname"",
                      ""versionname"",
                      ""cityname"",
                      ""publishdate"",
                      ""color"",
                      ""type"",
                      ""viptype""
                    ]
                  }}", (pageIndex - 1) * pageSize, pageSize, jsonSort);

            #endregion

            // Query
            strBuilder.Append(@",");
            // Begin Query
            strBuilder.Append(@"
                ""query"": {
                    ""bool"": 
                     {
                ");
            strBuilder.Append(@"""must"": [");

            strBuilder.Append(buildTermQuery("ispublish", true));

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = StringUtils.RemoveStrHtmlTags(keyword).Trim().ToLower();

                strBuilder.Append(",");
                strBuilder.AppendFormat(@"
                    {{
                        ""query_string"": {{
                            ""fuzziness"": ""AUTO"",
                            ""minimum_should_match"": 2,
                            ""fuzzy_rewrite"": ""constant_score_boolean"",
                            ""query"": ""{1}"",
                            ""fields"": [
                                ""{0}""
                            ],
                            ""default_operator"": ""or"",
                            ""analyzer"": ""standard"",
                            ""fuzzy_prefix_length"": 3,
                            ""phrase_slop"": 2,
                            ""lenient"": true,
                            ""_name"": ""textsearch_query"",
                            ""boost"": 1.1
                        }}
                    }}
                ", "textsearch", keyword);
            }

            if (haveImage == (int)FilterImage.HaveImage)
            {
                strBuilder.Append(",");
                strBuilder.Append(buildExistsQuery("image"));
            }

            strBuilder.Append("]");

            if (haveImage == (int)FilterImage.NoneImage)
            {
                strBuilder.Append(",");
                strBuilder.Append(@"""must_not"": [");
                strBuilder.Append(buildExistsQuery("image"));
                strBuilder.Append("]");
            }

            #endregion

            // End bool
            strBuilder.Append(@"
                         }
                ");
            // End query string
            strBuilder.Append(@"
                    }
                ");

            // End Query
            strBuilder.Append(@"
                }");

            if (strBuilder != null)
            {
                query = strBuilder.ToString();
            }

            ElasticsearchResponse<Models.ElasticSearchResponseData.ResponseData> response = _elasticClient.Search<Models.ElasticSearchResponseData.ResponseData>(_index, _typeProductPublish, query);

            if (response != null && response.Success)
            {
                Models.ElasticSearchResponseData.ResponseData responseData = response.Body;

                var hit = responseData.hits.hits;

                if (hit != null && hit.Any())
                {
                    productList = hit.Select(x => new ProductInfo(x._source)).ToList();
                }

                totalRecord = responseData.hits.total;
            }

            System.DateTime endDate = DateTime.Now;
            double totalMilliseconds = (endDate - fromDate).TotalMilliseconds;

            if (totalMilliseconds > 1000)
            {
                Logger<>.WriteLog(Logger<>.LogType.Trace, string.Format("_getListAutoByKeyword - {3} - timeleft: {0:n0}ms, took: {1} width query: {2}", totalMilliseconds, response.Body.took, Regex.Replace(query, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1"), guid));
            }

            return productList;
        }

        private string _genQueryStringForSearchProduct(SearchProductInfoES searchInfo)
        {
            string output = string.Empty;

            StringBuilder strBuilder = new StringBuilder();

            #region sort
            string jsonSort = string.Empty;
            Dictionary<string, string> dictSort = new Dictionary<string, string>();

            switch (searchInfo.arrangeId)
            {
                case (int)ArrangeId.PublishDate:
                    dictSort.Add("haveprice", "asc");
                    break;
                case (int)ArrangeId.PriceASC:
                    dictSort.Add("haveprice", "desc");
                    dictSort.Add("price", "asc");
                    break;
                case (int)ArrangeId.PriceDESC:
                    dictSort.Add("haveprice", "desc");
                    dictSort.Add("price", "desc");
                    break;
                case (int)ArrangeId.YearOldToNew:
                    dictSort.Add("year", "asc");
                    break;
                case (int)ArrangeId.YearNewToOld:
                    dictSort.Add("year", "desc");
                    break;
                case (int)ArrangeId.NumOfKmASC:
                    dictSort.Add("numofkm", "asc");
                    break;
                case (int)ArrangeId.NumOfKmDESC:
                    dictSort.Add("numofkm", "desc");
                    break;
                case (int)ArrangeId.CreateDateASC:
                    dictSort.Add("publishdate", "asc");
                    break;
                case (int)ArrangeId.CreateDateDESC:
                default:
                    //dictSort.Add("publishdate", "desc");
                    dictSort.Add("viptype", "desc");
                    dictSort.Add("publishdate", "desc");
                    break;
            }
            jsonSort = buildSortOrder(dictSort);

            if (!string.IsNullOrEmpty(jsonSort)) jsonSort = jsonSort + ",";

            #endregion

            strBuilder.AppendFormat(@"
                {{
                  ""from"": {0},
                  ""size"": {1},
                  {2}
                  ""_source"": {{
                    ""includes"": [
                      ""productid"",
                      ""title"",
                      ""maker"",
                      ""model"",
                      ""version"",
                      ""transmissionid"",
                      ""price"",
                      ""year"",
                      ""city"",
                      ""numofkm"",
                      ""numofkmunit"",
                      ""image"",
                      ""usertype"",
                      ""secondhand"",
                      ""createduser"",
                      ""createdate"",
                      ""ispublish"",
                      ""branchname"",
                      ""modelname"",
                      ""versionname"",
                      ""cityname"",
                      ""publishdate"",
                      ""color"",
                      ""type"",
                      ""viptype""
                    ]
                  }}", (searchInfo.pageIndex - 1) * searchInfo.pageSize, searchInfo.pageSize, jsonSort);

            strBuilder.Append(@",");

            if (searchInfo.LifeStyle == (int)LifeStyle.LCGC && !string.IsNullOrEmpty(Webconfig.ModelLCGC))
            {
                strBuilder.Append(buildFilterQuery("model", Webconfig.ModelLCGC));
            }
            else
            {
                // Begin Query
                strBuilder.Append(@"
                ""query"": {
                    ""bool"": 
                     {
                ");

                #region build query details by conditions must

                strBuilder.Append(@"""must"": [");

                strBuilder.Append(buildTermQuery("ispublish", true));

                if (searchInfo.CityId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("city", searchInfo.CityId));
                    searchInfo.countSearch++;
                }
                else
                {
                    if (searchInfo.RegionId > 0)
                    {
                        strBuilder.Append(",");
                        strBuilder.Append(buildTermQuery("region", searchInfo.RegionId));
                        searchInfo.countSearch++;
                    }
                }

                if (searchInfo.BrandId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("maker", searchInfo.BrandId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.ModelId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("model", searchInfo.ModelId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.VersionId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("version", searchInfo.VersionId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.TransmissionId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("transmissionid", searchInfo.TransmissionId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.DealerId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("createduser", searchInfo.DealerId));
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("usertype", searchInfo.DealerType));
                    searchInfo.countSearch++;
                }

                if (searchInfo.SecondHandId > 1)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("secondhand", searchInfo.SecondHandId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.ColorId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("color", searchInfo.ColorId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.TypeId > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildTermQuery("type", searchInfo.TypeId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.MinYear > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildRangeQuery("year", searchInfo.MinYear, Operator.GreaterThanOrEquals));
                    searchInfo.countSearch++;
                }

                if (searchInfo.MaxYear > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildRangeQuery("year", searchInfo.MaxYear, Operator.LessThanOrEquals));
                    searchInfo.countSearch++;
                }

                if (searchInfo.MinPrice > 0 && searchInfo.MaxPrice > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildRangeQuery("price", searchInfo.MinPrice, Operator.GreaterThanOrEquals));
                    strBuilder.Append(",");
                    strBuilder.Append(buildRangeQuery("price", searchInfo.MaxPrice, Operator.LessThanOrEquals));
                    searchInfo.countSearch++;
                }
                else
                {
                    if (searchInfo.MinPrice > 0)
                    {
                        strBuilder.Append(",");
                        strBuilder.Append(buildRangeQuery("price", searchInfo.MinPrice, Operator.GreaterThanOrEquals));
                        searchInfo.countSearch++;
                    }
                    if (searchInfo.MaxPrice > 0)
                    {
                        strBuilder.Append(",");
                        strBuilder.Append(buildRangeQuery("price", searchInfo.MaxPrice, Operator.LessThanOrEquals));
                        searchInfo.countSearch++;
                    }
                }

                if (searchInfo.NumOfKmMin > 0 && searchInfo.NumOfKmMax > 0)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMin, Operator.GreaterThanOrEquals));
                    strBuilder.Append(",");
                    strBuilder.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMax, Operator.LessThanOrEquals));
                    searchInfo.countSearch++;
                }
                else
                {
                    if (searchInfo.NumOfKmMin > 0)
                    {
                        strBuilder.Append(",");
                        strBuilder.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMin, Operator.GreaterThanOrEquals));
                        searchInfo.countSearch++;
                    }
                    if (searchInfo.NumOfKmMax > 0)
                    {
                        strBuilder.Append(",");
                        strBuilder.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMax, Operator.LessThanOrEquals));
                        searchInfo.countSearch++;
                    }
                }

                if (searchInfo.HaveImage == (int)FilterImage.HaveImage)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(buildExistsQuery("image"));
                }

                strBuilder.Append("]");

                #endregion

                #region  build query details by conditions must not

                if (searchInfo.HaveImage == (int)FilterImage.NoneImage)
                {
                    strBuilder.Append(",");
                    strBuilder.Append(@"""must_not"": [");
                    strBuilder.Append(buildExistsQuery("image"));
                    strBuilder.Append("]");
                }

                #endregion

                // End bool
                strBuilder.Append(@"
                         }
                ");
                // End query string
                strBuilder.Append(@"
                    }
                ");
            }
            // End Query
            strBuilder.Append(@"
                }");

            if (strBuilder != null)
            {
                output = strBuilder.ToString();

                //output = Regex.Replace(output, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
            }

            return output;
        }

        private SearchProductInfo _searchProductSuggest(SearchProductInfo searchInfo)
        {
            if (searchInfo.TransmissionId > 0)
            {
                searchInfo.TransmissionId = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.TypeId > 0)
            {
                searchInfo.TypeId = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.NumOfKmMin > 0 && searchInfo.NumOfKmMax > searchInfo.NumOfKmMin)
            {
                searchInfo.NumOfKmMin = 0;
                searchInfo.NumOfKmMax = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.ColorId > 0)
            {
                searchInfo.ColorId = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.MinPrice > 0 && searchInfo.MaxPrice > searchInfo.MinPrice)
            {
                searchInfo.MinPrice = 0;
                searchInfo.MaxPrice = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.MinYear > 0 && searchInfo.MinYear > searchInfo.MinYear)
            {
                searchInfo.MinYear = 0;
                searchInfo.MaxYear = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.ModelId > 0 || searchInfo.VersionId > 0)
            {
                searchInfo.ModelId = 0;
                searchInfo.VersionId = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.BrandId > 0)
            {
                searchInfo.BrandId = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }

            if (searchInfo.CityId > 0)
            {
                searchInfo.CityId = 0;
                if (CountSearchProduct(searchInfo) >= searchInfo.pageSize)
                {
                    return searchInfo;
                }
            }
            return searchInfo;
        }

        private string _genQueryStringForCountProductLeftMenu(SearchProductInfoES searchInfo)
        {
            string output = string.Empty;

            StringBuilder strBuilderCondition = new StringBuilder();

            if (searchInfo.LifeStyle == (int)LifeStyle.LCGC && !string.IsNullOrEmpty(Webconfig.ModelLCGC))
            {
                strBuilderCondition.Append(buildFilterQuery("model", Webconfig.ModelLCGC));
            }
            else
            {
                // Begin Query Condition
                strBuilderCondition.Append(@"
                ""query"": {
                    ""bool"": 
                     {
                ");

                #region build query details by conditions must

                strBuilderCondition.Append(@"""must"": [");

                strBuilderCondition.Append(buildTermQuery("ispublish", true));

                if (searchInfo.CityId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("city", searchInfo.CityId));
                    searchInfo.countSearch++;
                }
                else
                {
                    if (searchInfo.RegionId > 0)
                    {
                        strBuilderCondition.Append(",");
                        strBuilderCondition.Append(buildTermQuery("region", searchInfo.RegionId));
                        searchInfo.countSearch++;
                    }
                }

                if (searchInfo.BrandId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("maker", searchInfo.BrandId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.ModelId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("model", searchInfo.ModelId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.VersionId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("version", searchInfo.VersionId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.TransmissionId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("transmissionid", searchInfo.TransmissionId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.DealerId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("createduser", searchInfo.DealerId));
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("usertype", searchInfo.DealerType));
                    searchInfo.countSearch++;
                }

                if (searchInfo.SecondHandId > 1)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("secondhand", searchInfo.SecondHandId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.ColorId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("color", searchInfo.ColorId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.TypeId > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildTermQuery("type", searchInfo.TypeId));
                    searchInfo.countSearch++;
                }

                if (searchInfo.MinYear > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildRangeQuery("year", searchInfo.MinYear, Operator.GreaterThanOrEquals));
                    searchInfo.countSearch++;
                }

                if (searchInfo.MaxYear > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildRangeQuery("year", searchInfo.MaxYear, Operator.LessThanOrEquals));
                    searchInfo.countSearch++;
                }

                if (searchInfo.MinPrice > 0 && searchInfo.MaxPrice > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildRangeQuery("price", searchInfo.MinPrice, Operator.GreaterThanOrEquals));
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildRangeQuery("price", searchInfo.MaxPrice, Operator.LessThanOrEquals));
                    searchInfo.countSearch++;
                }
                else
                {
                    if (searchInfo.MinPrice > 0)
                    {
                        strBuilderCondition.Append(",");
                        strBuilderCondition.Append(buildRangeQuery("price", searchInfo.MinPrice, Operator.GreaterThanOrEquals));
                        searchInfo.countSearch++;
                    }
                    if (searchInfo.MaxPrice > 0)
                    {
                        strBuilderCondition.Append(",");
                        strBuilderCondition.Append(buildRangeQuery("price", searchInfo.MaxPrice, Operator.LessThanOrEquals));
                        searchInfo.countSearch++;
                    }
                }

                if (searchInfo.NumOfKmMin > 0 && searchInfo.NumOfKmMax > 0)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMin, Operator.GreaterThanOrEquals));
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMax, Operator.LessThanOrEquals));
                    searchInfo.countSearch++;
                }
                else
                {
                    if (searchInfo.NumOfKmMin > 0)
                    {
                        strBuilderCondition.Append(",");
                        strBuilderCondition.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMin, Operator.GreaterThanOrEquals));
                        searchInfo.countSearch++;
                    }
                    if (searchInfo.NumOfKmMax > 0)
                    {
                        strBuilderCondition.Append(",");
                        strBuilderCondition.Append(buildRangeQuery("numofkm", searchInfo.NumOfKmMax, Operator.LessThanOrEquals));
                        searchInfo.countSearch++;
                    }
                }

                if (searchInfo.HaveImage == (int)FilterImage.HaveImage)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(buildExistsQuery("image"));
                }

                strBuilderCondition.Append("]");

                #endregion

                #region  build query details by conditions must not

                if (searchInfo.HaveImage == (int)FilterImage.NoneImage)
                {
                    strBuilderCondition.Append(",");
                    strBuilderCondition.Append(@"""must_not"": [");
                    strBuilderCondition.Append(buildExistsQuery("image"));
                    strBuilderCondition.Append("]");
                }

                #endregion

                // End query Condition
                strBuilderCondition.Append(@"
                        }
                    }
                ");
            }

            StringBuilder strBuilderAggregate = new StringBuilder();

            strBuilderAggregate.Append(@"
                ""aggs"": {
            ");

            if (searchInfo.RegionId > 0)
            {
                strBuilderAggregate.Append(buildAggGroup("group_by_city", "city"));
            }
            else
            {
                strBuilderAggregate.Append(buildAggGroup("group_by_region", "region"));
            }

            if (searchInfo.ModelId > 0)
            {
                strBuilderAggregate.Append(",");
                strBuilderAggregate.Append(buildAggGroup("group_by_version", "version"));
            }
            else if (searchInfo.BrandId > 0)
            {
                strBuilderAggregate.Append(",");
                strBuilderAggregate.Append(buildAggGroup("group_by_model", "model"));
            }
            else
            {
                strBuilderAggregate.Append(",");
                strBuilderAggregate.Append(buildAggGroup("group_by_maker", "maker"));
            }

            if (searchInfo.SecondHandId <= 0)
            {
                strBuilderAggregate.Append(",");
                strBuilderAggregate.Append(buildAggGroup("group_by_secondhand", "secondhand"));
            }

            if (searchInfo.TransmissionId <= 0)
            {
                strBuilderAggregate.Append(",");
                strBuilderAggregate.Append(buildAggGroup("group_by_transmissionid", "transmissionid"));
            }

            if (searchInfo.ColorId <= 0)
            {
                strBuilderAggregate.Append(",");
                strBuilderAggregate.Append(buildAggGroup("group_by_color", "color"));
            }

            if (searchInfo.TypeId <= 0)
            {
                strBuilderAggregate.Append(",");
                strBuilderAggregate.Append(buildAggGroup("group_by_type", "type"));
            }

            strBuilderAggregate.Append(@"
                }
            ");

            StringBuilder strBuilderQuery = new StringBuilder();
            strBuilderQuery.AppendFormat(@"
                {{
                    ""size"": 0,
                    {0},
                    {1}
                }}", strBuilderCondition.ToString(), strBuilderAggregate);

            output = strBuilderQuery.ToString();

            //output = Regex.Replace(output, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");

            return output;
        }

        private ProductInfo _convertHitToProductInfo(IHit<ProductInfo> hit)
        {
            Func<IHit<ProductInfo>, ProductInfo> func = (x) =>
            {
                hit.Source.Id = Convert.ToInt32(hit.Id);
                return hit.Source;
            };

            return func.Invoke(hit);
        }

        private string buildTermQuery<T>(string fieldName, T objValue)
        {
            string value = string.Empty;

            if (typeof(T) == typeof(string))
            {
                value = string.Format("\"{0}\"", objValue);
            }
            else
            {
                value = objValue.ToString().ToLower();
            }

            return string.Format(@"
                {{
                    ""term"": 
                    {{
                        ""{0}"": 
                        {{
                            ""value"": {1}
                        }}
                    }}
                }}
            ", fieldName, value);
        }

        private string buildExistsQuery(string fieldName)
        {
            return string.Format(@"
                {{
                    ""exists"": 
                    {{
                        ""field"": ""{0}""
                    }}
                }}
            ", fieldName);
        }

        private string buildFilterQuery(string fieldName, string arrValue)
        {
            if (string.IsNullOrEmpty(arrValue)) return string.Empty;

            return string.Format(@"
               ""filtered"" : 
                {{
                    ""filter"" : 
                    {{
                        ""terms"" : 
                        {{ 
                            ""{0}"" : [{1}]
                        }}
                    }}
                }}
            ", fieldName, arrValue.TrimStart(',').TrimEnd(','));
        }

        private string buildSortOrder(string field, string order)
        {
            return string.Format(@"
                    {{
                        ""{0}"": 
                        {{
                            ""order"": ""{1}""
                        }}
                    }}
                ", field, order);
        }

        private string buildSortOrder(Dictionary<string, string> fields)
        {
            string outputSort = string.Empty;

            if (fields == null || fields.Count() <= 0) return outputSort;

            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append(@"
                ""sort"": 
                [
            ");
            bool isFrist = true;

            foreach (var item in fields)
            {
                if (!isFrist)
                {
                    strBuilder.Append(",");
                }
                strBuilder.Append(buildSortOrder(item.Key, item.Value));

                isFrist = false;
            }

            strBuilder.Append(@"
                ]                
            ");

            outputSort = strBuilder.ToString();

            return outputSort;
        }

        private string buildRangeQuery(string fieldName, object value, Operator operater)
        {
            if (value != null)
            {
                value = value.ToString().Replace(",", ".");
            }

            return string.Format(@"
                {{
                    ""range"": 
                    {{
                        ""{0}"": 
                        {{
                            ""{1}"": {2}
                        }}
                    }}
                }}
            ", fieldName, GetEnumDescription(operater), value);
        }

        private string buildAggGroup(string groupName, string fieldName)
        {
            return string.Format(@"
                ""{0}"": {{
	                ""terms"": {{
	                    ""field"": ""{1}"",
	                    ""size"": 0
	                }}
                }}
            ", groupName, fieldName);
        }

        private enum Operator
        {
            [Description("eq")]
            Equals = 0,
            [Description("gt")]
            GreaterThan = 1,
            [Description("gte")]
            GreaterThanOrEquals = 2,
            [Description("lt")]
            LessThan = 3,
            [Description("lte")]
            LessThanOrEquals = 4,
        }

        #endregion
        public static string GetEnumDescription(Enum value)
        {
            try
            {
                FieldInfo fi = value.GetType().GetField(value.ToString());

                DescriptionAttribute[] attributes =
                    (DescriptionAttribute[])fi.GetCustomAttributes(
                        typeof(DescriptionAttribute),
                        false);

                if (attributes != null &&
                    attributes.Length > 0)
                    return attributes[0].Description;
                else
                    return value.ToString();
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        public enum FilterImage
        {
            AllImage = 0,
            HaveImage = 1,
            NoneImage = 2
        }
        public enum LifeStyle
        {
            LCGC = 1
        }
    }
}
