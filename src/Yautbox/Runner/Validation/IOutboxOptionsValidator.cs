using System.Collections.Generic;
using Yautbox.Runner.Options;

namespace Yautbox.Runner.Validation;

/// <summary>
/// Checks the <see cref="IOutboxRunnerOptions{TPayload}"/> the outbox runners of <typeparamref name="TPayload"/>
/// are about to work with.
/// </summary>
/// <remarks>
/// Every validator registered for <typeparamref name="TPayload"/> runs when a runner starts and again after every
/// options reload, and their results are combined. Any <see cref="ValidateResult.Failure"/> is fatal: all failures
/// are logged at <c>Error</c> level and the runner stops instead of restarting, which with the default
/// <c>BackgroundServiceExceptionBehavior.StopHost</c> stops the application. A <see cref="ValidateResult.Warning"/>
/// is only logged.
/// </remarks>
/// <typeparam name="TPayload">Payload type whose options are validated.</typeparam>
internal interface IOutboxOptionsValidator<TPayload>
{
    /// <summary>
    /// Validates <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The options a runner is about to work with.</param>
    /// <returns>One result per problem found; empty when the options are valid.</returns>
    IEnumerable<ValidateResult> Validate(IOutboxRunnerOptions<TPayload> options);
}
