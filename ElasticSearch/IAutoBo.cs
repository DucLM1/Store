using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearch
{
    public interface IAutoBo
    {
        List<ProductInfo> SearchProduct(SearchProductInfo searchInfo, out int totalRecord, out int totalRecordSuggest, int limit = 3, Guid guid = new Guid());
        int CountSearchProduct(SearchProductInfo searchInfo);
        List<ProductInfo> GetListAutoSameType(short brandId, short modelId, short cityId, double minPrice, double maxPrice, int ignoreProductId, int top, Guid guid = new Guid());
        List<ProductInfo> GetListAutoSamePrice(short brandId, short cityId, double minPrice, double maxPrice, List<int> ignoreProductIds, int top, Guid guid = new Guid());
        List<ProductInfo> GetListAutoByKeyword(string keyword, int haveImage, int pageIndex, int pageSize, int orderBy, out int totalRecord, Guid guid = new Guid());
        Models.ESAggResponseData.Aggregation CountProductForLeftMenu(SearchProductInfo searchInfo);
    }
}
