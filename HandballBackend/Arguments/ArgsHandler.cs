namespace HandballBackend.Arguments;

public static class ArgsHandler {
    public static readonly AbstractArgumentHandler[] Handlers = [
        new WorkingDirectoryArgHandler(),
        new LoggingArgHandler(),
        new PortArgHandler(),
        new WorkingDirectoryArgHandler(),
        new GitArgHandler(),
        new HelpArgHandler(),
        new BackupArgHandler(),
    ];

    public static void Parse(string[] args, WebApplicationBuilder builder) {
        var index = 0;
        while (index < args.Length) {
            var fail = true;
            foreach (var handler in Handlers) {
                if (handler.Parse(args, ref index, builder)) {
                    fail = false;
                    break;
                }
            }

            if (fail) {
                Console.WriteLine($"Unrecognised Argument {args[index]}.  Run with --help for more information.");
                break;
            }
        }
    }
}

public abstract class AbstractArgumentHandler(string shortName, string longName, string description) {
    public string ShortName { get; init; } = shortName;
    public string LongName { get; init; } = longName;
    public string Description { get; init; } = description;

    public virtual bool Parse(string[] args, ref int index, WebApplicationBuilder builder) {
        if (args[index] != $"-{ShortName}" && args[index] != $"--{LongName}") return false;
        index++;
        ParseIfMatched(args, ref index, builder);
        return true;
    }

    protected abstract void ParseIfMatched(string[] args, ref int index, WebApplicationBuilder builder);
}