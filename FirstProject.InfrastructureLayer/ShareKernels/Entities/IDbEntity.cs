namespace FirstProject.InfrastructureLayer.ShareKernels.Entities
{
    public interface IDbEntity<TId>
    {
        TId GetId();

        void SetId(TId id);
    }

    public interface IDbEntity : IDbEntity<int>
    {
    }
}