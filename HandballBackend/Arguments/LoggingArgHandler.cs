namespace HandballBackend.Arguments;

public class LoggingArgHandler()
    : AbstractArgumentHandler("l", "logRequest", "Logs Requests that the server receives.") {
    protected override void ParseIfMatched(string[] args, ref int index, WebApplicationBuilder builder) {
        if (index < args.Length && ((string[]) ["true", "false"]).Contains(args[index])) {
            Config.REQUEST_LOGGING = args[index++] == "true";
        } else {
            Config.REQUEST_LOGGING = true;
        }
    }
}