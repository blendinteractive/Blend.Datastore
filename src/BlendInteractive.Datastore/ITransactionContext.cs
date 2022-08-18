namespace BlendInteractive.Datastore
{
    public interface ITransactionContext
    {
        bool RollbackTransaction { get; set; }
    }
}
