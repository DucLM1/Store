namespace FirstProject.Data.Interfaces
{
    /// <summary>
    /// Dành cho triển khai nhiều ngôn ngữ
    /// </summary>
    public interface IMultiLanguage<TId>
    {
        TId LanguageId { get; set; }
    }
}