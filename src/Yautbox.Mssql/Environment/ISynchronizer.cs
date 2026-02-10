using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Mssql.Environment;

internal interface ISynchronizer
{
    Task ReadyAsync(CancellationToken cancellationToken);
}
