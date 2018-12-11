using ServiceStack.DataAnnotations;// ServiceStack.Redis Nuget Package
namespace FirstProject.InfrastructureLayer.ShareKernels.Entities
{
    /// <summary>
    /// Sử dụng khi code theo Code First, xây dựng DB mỗi table có 1 thằng Id là Primarikey
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public class DbEntity<TId> : DbEntityBase<TId>
    {
        [PrimaryKey]
        public virtual TId Id { get; set; }
    }

    public class DbEntity : DbEntity<int>
    {
    }
}
