namespace HandballBackend.ErrorTypes;

public class DoesNotExist(string type, string name)
    : ErrorType(nameof(DoesNotExist), $"{type} {name} does not exist", code: 404);