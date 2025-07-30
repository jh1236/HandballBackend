namespace HandballBackend.ErrorTypes;

public class ActionNotAllowed(string message) : ErrorType(nameof(ActionNotAllowed), message, 400);