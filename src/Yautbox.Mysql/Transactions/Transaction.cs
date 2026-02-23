using System;

namespace Yautbox.Mysql.Transactions;

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
