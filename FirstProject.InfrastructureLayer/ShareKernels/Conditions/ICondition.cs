using System;
using System.Collections.Generic;

namespace FirstProject.InfrastructureLayer.ShareKernels.Conditions
{
    public interface ICondition
    {
        IReadOnlyDictionary<string, Tuple<Type, object>> Conditions { get; }
    }
}