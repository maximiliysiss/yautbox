namespace Yautbox.Runner.Validation;

/// <summary>
/// A problem an <see cref="IOutboxOptionsValidator{TPayload}"/> found in the options: either a
/// <see cref="Failure"/> or a <see cref="Warning"/>.
/// </summary>
internal abstract record ValidateResult
{
    private ValidateResult()
    {
    }

    /// <summary>
    /// An option value the runners cannot work with. Processing of the payload type stops.
    /// </summary>
    /// <param name="Error">What is wrong, logged at <c>Error</c> level.</param>
    public sealed record Failure(string Error) : ValidateResult;

    /// <summary>
    /// An option value the runners work with, but that probably does not do what was intended.
    /// </summary>
    /// <param name="Message">What is questionable, logged at <c>Warning</c> level.</param>
    public sealed record Warning(string Message) : ValidateResult;
}
