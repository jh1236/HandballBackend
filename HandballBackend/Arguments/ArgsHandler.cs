﻿namespace HandballBackend.Arguments;

public static class ArgsHandler {
    public static AbstractArgumentHandler[] Handlers = [
        new HelpArgumentHandler(),
        new LoggingArgumentHandler(),
        new PortArgumentHandler()
    ];

    public static void Parse(string[] args, WebApplicationBuilder builder) {
        var index = 0;
        while (index < args.Length) {
            foreach (var handler in Handlers) {
                if (handler.Parse(args, ref index, builder)) break;
            }
        }
    }
}

public abstract class AbstractArgumentHandler {
    public string ShortName { get; init; }
    public string LongName { get; init; }
    public string Description { get; init; }

    public AbstractArgumentHandler(string shortName, string longName, string description) {
        ShortName = shortName;
        LongName = longName;
        Description = description;
    }

    public virtual bool Parse(string[] args, ref int index, WebApplicationBuilder builder) {
        if (args[index] != $"-{ShortName}" && args[index] != $"--{LongName}") return false;
        index++;
        ParseIfMatched(args, ref index, builder);
        return true;
    }

    protected abstract void ParseIfMatched(string[] args, ref int index, WebApplicationBuilder builder);
}