using System.Web.WebPages;
using HandballBackend.EndpointHelpers;

namespace HandballBackend.Arguments;

public class GitArgHandler() : AbstractArgumentHandler("u", "update", "Automatically updates the program.") {
    protected override void ParseIfMatched(string[] args, ref int index, WebApplicationBuilder builder) {
        var force = false;
        var frequency = 60 * 60;
        if (index < args.Length) {
            if (args[index] == "force") {
                force = true;
                index++;
            }

            else if (args[index].IsInt()) {
                frequency = int.Parse(args[index]);
            }
        }

        GitHelper.StartCheckingForUpdates(force, frequency);
    }
}