using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Mysql.Environment;

internal interface ISynchronizer
{
    Task ReadyAsync(CancellationToken cancellationToken);
}
