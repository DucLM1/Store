namespace FirstProject.Data.Interfaces
{
    /// <summary>
    /// Dành cho triển khai Sort
    /// </summary>
    public interface ISortable
    {
        int SortOrder { get; set; }
    }
}