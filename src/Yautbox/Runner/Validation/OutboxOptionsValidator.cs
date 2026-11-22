using System;
using System.Collections.Generic;
using System.Linq;
using Yautbox.Runner.Options;

namespace Yautbox.Runner.Validation;

internal sealed class OutboxOptionsValidator<TPayload> : IOutboxOptionsValidator<TPayload>
{
    public IEnumerable<ValidateResult> Validate(IOutboxRunnerOptions<TPayload> options)
    {
        if (options.PollDelay <= TimeSpan.Zero)
            yield return new ValidateResult.Failure("PollDelay must be greater than zero");

        if (options.HandleTimeout <= TimeSpan.Zero)
            yield return new ValidateResult.Failure("HandleTimeout must be greater than zero");

        if (options.Visibility <= TimeSpan.Zero)
            yield return new ValidateResult.Failure("Visibility must be greater than zero");

        if (options.BufferSize <= 0)
            yield return new ValidateResult.Failure("BufferSize must be greater than zero");

        if (options.PerBufferCount <= 0)
            yield return new ValidateResult.Failure("PerBufferCount must be greater than zero");

        if (options.PerBufferCount > options.BufferSize)
            yield return new ValidateResult.Failure("PerBufferCount must be less than or equal to BufferSize");

        if (options.WorkersCount <= 0)
            yield return new ValidateResult.Failure("WorkersCount must be greater than zero");

        if (options.FailureDelay <= TimeSpan.Zero)
            yield return new ValidateResult.Failure("FailureDelay must be greater than zero");

        if (options.RetryCount is <= 0)
            yield return new ValidateResult.Failure("RetryCount must be greater than zero");

        if (options.RetryDelays.Any(delay => delay <= TimeSpan.Zero))
            yield return new ValidateResult.Failure("RetryDelays must be greater than zero");

        if (options.BackupInterval <= TimeSpan.Zero)
            yield return new ValidateResult.Failure("BackupInterval must be greater than zero");

        if (options.CleanupInterval <= TimeSpan.Zero)
            yield return new ValidateResult.Failure("CleanupInterval must be greater than zero");

        if (options.PolicyTimeout <= TimeSpan.Zero)
            yield return new ValidateResult.Failure("PolicyTimeout must be greater than zero");

        if (options.ExecutionPolicy is ExecutionPolicy.Sequential && options.PerBufferCount != options.BufferSize)
            yield return new ValidateResult.Failure("Sequential execution policy requires PerBufferCount to be equal to BufferSize");

        if (options is { ExecutionPolicy: ExecutionPolicy.Sequential, RetryCount: null, RetryDelays: [] })
        {
            yield return new ValidateResult.Warning(
                "Sequential execution policy without RetryCount and RetryDelays retries a failing message indefinitely " +
                "and holds back the messages after it until it succeeds");
        }

        if (options.PartitionKey is not null && options.ScopeLifetime is not ScopeLifetime.PerBatch)
        {
            yield return new ValidateResult.Warning(
                "PartitionKey with ScopeLifetime PerGroup or PerSession shares one handler instance between concurrent " +
                "HandleAsync calls; the handler and its scoped dependencies must be thread-safe");
        }
    }
}
