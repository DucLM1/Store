namespace FirstProject.Data.Interfaces
{
    /// <summary>
    /// Dành cho triển khai với những DB không xóa thực mà chỉ set delete = 1
    /// </summary>
    public interface IHasSoftDelete
    {
        bool IsDeleted { get; set; }
    }
}