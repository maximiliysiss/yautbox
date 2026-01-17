using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Extensions.Enumerable;

internal static class ReadonlyCollectionExtensions
{
    public static async IAsyncEnumerable<T[]> ChunkAsync<T>(
        this Task<IReadOnlyCollection<T>> task,
        int size,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerable = await task;

        foreach (var chunk in enumerable.Chunk(size))
            yield return chunk;
    }
}
