namespace FirstProject.Data.Interfaces
{
    /// <summary>
    /// Dành cho triển khai với project có người tạo
    /// </summary>
    public interface IHasOwner<TId>
    {
        TId OwnerId { get; set; }
    }
}