using ServiceStack.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FirstProject.InfrastructureLayer.ShareKernels.Conditions
{
    /// <summary>
    /// Class chứa thông tin của tất cả các condition
    /// </summary>
    public class Condition : ICondition
    {
        private Dictionary<string, Tuple<Type, object>> _conditions;

        private void CreateConditions()
        {
            _conditions = new Dictionary<string, Tuple<Type, object>>();
            var props = GetType().GetProperties();
            foreach (var propertyInfo in props)
            {
                if (propertyInfo.Name != "Conditions" && propertyInfo.GetCustomAttributes<IgnoreAttribute>() == null)
                {
                    var val = propertyInfo.GetValue(this);
                    _conditions.Add("_" + propertyInfo.Name.ToLower(), new Tuple<Type, object>(propertyInfo.PropertyType, val));
                }
            }
        }
        [Ignore]
        public IReadOnlyDictionary<string, Tuple<Type, object>> Conditions
        {
            get
            {
                if (_conditions == null)
                    CreateConditions();
                return _conditions;
            }
        }
    }
}