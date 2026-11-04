namespace Atc.Hosting.Internal;

/// <summary>
/// Returns the options instance registered by <c>AddBackgroundService</c> or <c>AddBackgroundScheduleService</c> for every name,
/// so <see cref="IOptions{TOptions}"/>, <see cref="IOptionsMonitor{TOptions}"/> and <see cref="IOptionsSnapshot{TOptions}"/>
/// all resolve the same bound instance instead of a new default one.
/// </summary>
/// <remarks>
/// The instance is already bound, so <see cref="IConfigureOptions{TOptions}"/> and <see cref="IPostConfigureOptions{TOptions}"/> don't run.
/// <see cref="IValidateOptions{TOptions}"/> does, the same way the default factory runs it, so validators and <c>ValidateOnStart</c> keep working.
/// </remarks>
/// <typeparam name="TOptions">The options type.</typeparam>
internal sealed class RegisteredOptionsFactory<TOptions> : IOptionsFactory<TOptions>
    where TOptions : class
{
    private readonly TOptions options;
    private readonly IValidateOptions<TOptions>[] validators;

    public RegisteredOptionsFactory(
        TOptions options,
        IEnumerable<IValidateOptions<TOptions>> validators)
    {
        this.options = options;
        this.validators = validators.ToArray();
    }

    public TOptions Create(string name)
    {
        var failures = new List<string>();

        foreach (var validator in validators)
        {
            var result = validator.Validate(name, options);

            if (result.Failed)
            {
                failures.AddRange(result.Failures);
            }
        }

        if (failures.Count > 0)
        {
            throw new OptionsValidationException(name, typeof(TOptions), failures);
        }

        return options;
    }
}