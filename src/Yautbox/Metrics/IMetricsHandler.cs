using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yautbox.Metrics;

/// <summary>
/// Receives outbox lifecycle metrics and timing information.
/// </summary>
public interface IMetricsHandler
{
    /// <summary>
    /// Reports that messages were added to the outbox.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="count">Number of messages added.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask AddedAsync(string identifier, int count, CancellationToken cancellationToken);

    /// <summary>
    /// Reports that messages were canceled.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="count">Number of messages canceled.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask CanceledAsync(string identifier, int count, CancellationToken cancellationToken);

    /// <summary>
    /// Reports that messages were handled and how long the handling took.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="count">Number of messages handled.</param>
    /// <param name="elapsed">Elapsed handling time.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask HandledAsync(string identifier, int count, TimeSpan elapsed, CancellationToken cancellationToken);

    /// <summary>
    /// Reports that messages were scheduled for retry.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="count">Number of messages retried.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask RetriedAsync(string identifier, int count, CancellationToken cancellationToken);

    /// <summary>
    /// Reports that messages were deleted from the outbox.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="count">Number of messages deleted.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask DeletedAsync(string identifier, int count, CancellationToken cancellationToken);

    /// <summary>
    /// Reports the time spent cleaning up old handled messages.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="elapsed">Elapsed cleanup time.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask CleanedInAsync(string identifier, TimeSpan elapsed, CancellationToken cancellationToken);

    /// <summary>
    /// Reports the time spent reading messages from storage.
    /// </summary>
    /// <param name="identifier">Outbox handler identifier.</param>
    /// <param name="elapsed">Elapsed read time.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    ValueTask ReadedInAsync(string identifier, TimeSpan elapsed, CancellationToken cancellationToken);
}
