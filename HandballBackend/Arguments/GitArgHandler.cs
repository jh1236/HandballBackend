using System.Text;
using HandballBackend.EndpointHelpers;

namespace HandballBackend.Arguments;

public class GitArgHandler() : AbstractArgumentHandler("u", "update", "Automatically updates the program.") {
    protected override void ParseIfMatched(string[] args, ref int index, WebApplicationBuilder builder) {
        GitHelper.StartCheckingForUpdates();
    }
}