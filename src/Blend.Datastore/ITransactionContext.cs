namespace Blend.Datastore
{
    public interface ITransactionContext
    {
        bool RollbackTransaction { get; set; }
    }
}
