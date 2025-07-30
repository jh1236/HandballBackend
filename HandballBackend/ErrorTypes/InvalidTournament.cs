namespace HandballBackend.ErrorTypes;

public class InvalidTournament(string providedName)
    : ErrorType(nameof(InvalidTournament), $"Tournament {providedName} does not exist", code: 404);