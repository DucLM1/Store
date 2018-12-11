namespace FirstProject.InfrastructureLayer.ShareKernels.Conditions
{
    public class IdCondition<TId> : Condition
    {
        public TId Id { get; set; }
    }
}