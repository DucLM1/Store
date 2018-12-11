using FirstProject.InfrastructureLayer.ShareKernels.Conditions;
using FirstProject.InfrastructureLayer.ShareKernels.Entities;
using System.Collections.Generic;

namespace FirstProject.InfrastructureLayer.ShareKernels.DAL
{
    public interface IEntityQueryDal<T, TId> where T : IDbEntity<TId>
    {
        T GetById(TId id);

        IEnumerable<T> GetAll();

        IEnumerable<T> List(ICondition condition);

        int CountTotalRecord(ICondition condition);
    }
}