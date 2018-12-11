using ServiceStack.DataAnnotations;
using System.Reflection;

namespace FirstProject.InfrastructureLayer.ShareKernels.Entities
{
    /// <summary>
    /// DbEntityBase được sử dụng cho những bảng mà Primary Key không phải Id, ví dụ productId.
    /// Thằng này chỉ dùng nếu code theo DB First
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public class DbEntityBase<TId> : IDbEntity<TId>
    {
        private PropertyInfo _idInfo;

        public DbEntityBase()
        {
            if (_idInfo == null) _idInfo = GetIdInfo();
        }

        private PropertyInfo GetIdInfo()
        {
            if (_idInfo != null) return _idInfo;
            var props = this.GetType().GetProperties();
            foreach (var propertyInfo in props)
            {
                if (propertyInfo.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                {
                    _idInfo = propertyInfo;
                    break;
                }
            }
            return _idInfo;
        }

        public TId GetId()
        {
            if (_idInfo != null) return (TId)_idInfo.GetValue(this);
            return default(TId);
        }

        public void SetId(TId id)
        {
            if (_idInfo != null)
                _idInfo.SetValue(this, id);
        }
    }
}