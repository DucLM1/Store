using FirstProject.InfrastructureLayer.ShareKernels.Conditions;
using FirstProject.InfrastructureLayer.ShareKernels.Entities;

namespace FirstProject.InfrastructureLayer.ShareKernels.DAL
{
    public interface ICommandDal<TEntity, TId> where TEntity : IDbEntity<TId>
    {
        void Add(TEntity entity);

        int AddGetId(TEntity entity);

        void Update(TEntity entity);

        void DeleteById(TId id);

        void SetWriteDbContext(IDbContext writeContext);

        void Delete(ICondition condition);
    }

    public interface ICommandDal<TEntity> : ICommandDal<TEntity, int> where TEntity : IDbEntity
    {
    }
}