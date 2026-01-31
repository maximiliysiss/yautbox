namespace Yautbox.InMemory.Transactions;

internal static class Transaction
{
    public static System.Transactions.Transaction? Current()
    {
        try
        {
            return System.Transactions.Transaction.Current;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
