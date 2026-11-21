using System;

namespace Yautbox.Infrastructure.Hosted;

/// <summary>
/// Tells <see cref="RestartableService"/> to abandon its execution loop instead of restarting it.
/// </summary>
/// <remarks>
/// Every other exception is treated as transient: the restart policy waits and runs the loop again, forever.
/// Throw this one when another attempt cannot help (an invalid configuration, for instance). The exception leaves
/// the service's execution task faulted, so the host reacts to it as to any other background service failure:
/// with the default <c>BackgroundServiceExceptionBehavior.StopHost</c> the application stops. Log the reason before throwing.
/// </remarks>
/// <param name="message">The reason the loop cannot continue.</param>
internal sealed class InterruptLoopException(string message) : Exception(message);
