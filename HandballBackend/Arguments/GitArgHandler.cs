using HandballBackend.EndpointHelpers;

namespace HandballBackend.Arguments;

public class GitArgHandler() : AbstractArgumentHandler("u", "update", "Automatically updates the program.") {
    protected override void ParseIfMatched(string[] args, ref int index, WebApplicationBuilder builder) {
        var force = false;
        if (index < args.Length && args[index] == "force") {
            force = true;
            index++;
        }
        GitHelper.StartCheckingForUpdates(force);
    }
}