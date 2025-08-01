namespace HandballBackend.ErrorTypes;

public class MustProvideArgument(params string[] argumentName)
    : ErrorType(nameof(MustProvideArgument),
        $"At least one argument from [{string.Join(", ", argumentName)}] must be provided.");