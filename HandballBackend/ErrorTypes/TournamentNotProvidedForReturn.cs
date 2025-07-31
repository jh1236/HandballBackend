namespace HandballBackend.ErrorTypes;

public class TournamentNotProvidedForReturn()
    : ErrorType(nameof(TournamentNotProvidedForReturn), "Tournament must be provided when it is set to be returned.");