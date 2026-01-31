namespace Yautbox.Extensions.Options;

internal abstract record ValidationResult
{
    public sealed record SuccessValidationResult : ValidationResult;

    public sealed record FailureValidationResult(string ErrorMessage) : ValidationResult;
}
