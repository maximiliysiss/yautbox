using System;
using System.Linq;
using Yautbox.Runner.Options;

namespace Yautbox.Extensions.Options;

internal static class OutboxRunnerOptionsExtensions
{
    public static ValidationResult Validate(this IOutboxRunnerOptions options)
    {
        if (options.PollDelay <= TimeSpan.Zero)
            return new ValidationResult.FailureValidationResult("Poll delay cannot be zero or negative");

        if (options.HandleTimeout <= TimeSpan.Zero)
            return new ValidationResult.FailureValidationResult("Handle timeout cannot be zero or negative");

        if (options.Visibility <= TimeSpan.Zero)
            return new ValidationResult.FailureValidationResult("Visibility cannot be zero or negative");

        if (options.BufferSize <= 0)
            return new ValidationResult.FailureValidationResult("Buffer size cannot be zero or negative");

        if (options.PerBufferCount <= 0)
            return new ValidationResult.FailureValidationResult("Per buffer count cannot be zero or negative");

        if (options.WorkersCount <= 0)
            return new ValidationResult.FailureValidationResult("Workers count cannot be zero or negative");

        if (options.FailureDelay <= TimeSpan.Zero)
            return new ValidationResult.FailureValidationResult("Failure delay cannot be zero or negative");

        if (options.RetryCount < 0)
            return new ValidationResult.FailureValidationResult("Retry count cannot be negative");

        if (options.RetryDelays.Any(delay => delay < TimeSpan.Zero))
            return new ValidationResult.FailureValidationResult("Retry delays cannot be negative");

        if (options.BackupInterval is not null && options.BackupInterval <= TimeSpan.Zero)
            return new ValidationResult.FailureValidationResult("Backup interval cannot be zero or negative");

        if (options.ExecutionPolicy is ExecutionPolicy.Sequential && options.WorkersCount > 1)
            return new ValidationResult.FailureValidationResult("Sequential execution policy cannot be used with more than one worker");

        if (options.ExecutionPolicy is ExecutionPolicy.Sequential && options.PerBufferCount != options.BufferSize)
            return new ValidationResult.FailureValidationResult(
                "Sequential execution policy requires buffer size to be equal to per buffer count");

        return new ValidationResult.SuccessValidationResult();
    }
}
