namespace FirstProject.Data.Interfaces
{
    /// <summary>
    /// Dành cho triển khai SEO
    /// </summary>
    public interface IHasSeoMetaData
    {
        //Title của trang
        string SeoPageTitle { get; set; }

        //Path của trang (chỉ tính riêng phần gắn với cshtml, hoặc phần cuối cùng của Path sau dấu /)
        string SeoAlias { get; set; }

        string SeoKeywords { get; set; }
        string SeoDescription { get; set; }
    }
}