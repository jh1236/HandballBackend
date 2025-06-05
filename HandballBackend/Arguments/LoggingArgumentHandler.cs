namespace HandballBackend.Arguments;

public class LoggingArgumentHandler() : AbstractArgumentHandler("l", "logRequest", "Logs Requests that the server receives.") {
    protected override void ParseIfMatched(string[] args, ref int index, WebApplicationBuilder builder) {
        Config.REQUEST_LOGGING = true;
    }
}