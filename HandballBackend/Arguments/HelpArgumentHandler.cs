using System.Text;

namespace HandballBackend.Arguments;

public class HelpArgumentHandler()
    : AbstractArgumentHandler("h", "help", "Displays this help text.") {
    protected override void ParseIfMatched(
        string[] args,
        ref int index,
        WebApplicationBuilder builder
    ) {
        var strings = new List<(string, string)>();
        var longestArg = 0;
        foreach (var handler in ArgsHandler.Handlers) {
            var title = $"-{handler.ShortName}, --{handler.LongName}";
            var description = handler.Description;
            longestArg = Math.Max(title.Length, longestArg);
            strings.Add((title, description));
        }

        Console.WriteLine("Usage:");
        foreach (var (title, description) in strings) {
            Console.WriteLine($"   {title.PadRight(longestArg + 5)}: {description}");
        }

        Environment.Exit(0);
    }
}