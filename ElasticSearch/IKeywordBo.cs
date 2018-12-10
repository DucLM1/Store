
using System.Collections.Generic;

namespace ElasticSearch
{
    public interface IKeywordBo
    {
        List<KeywordEntityForSale> GetKeywordOnPageForSale(SearchKeywordForSaleInfo searchInfo, int productId, int top, ref string header, short type = 1, short year = 0, short limit = 9);


        List<KeywordEntityForSale> GetKeywordOnPageForSaleNoCached(SearchKeywordForSaleInfo searchInfo, int productId, int top, ref string header, short type = 1, short year = 0, short limit = 9);
    }
}
