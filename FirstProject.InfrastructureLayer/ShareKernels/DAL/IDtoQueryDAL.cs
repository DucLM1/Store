using FirstProject.InfrastructureLayer.ShareKernels.Conditions;
using FirstProject.InfrastructureLayer.ShareKernels.DTO;
using System.Collections.Generic;

namespace FirstProject.InfrastructureLayer.ShareKernels.DAL
{
    public interface IDtoQueryDal<T, TId> where T : IDto
    {
        IEnumerable<T> List(ICondition condition);

        T GetById(TId id);
    }

    public interface IDtoQueryDal<T> : IDtoQueryDal<T, int> where T : IDto
    {
    }
}