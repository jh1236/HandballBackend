using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HandballBackend.EndpointHelpers;

public static class ExceptionLoggingHelper {
    private const string Path = "./error.log";

    public static async Task Write(Exception exception, HttpContext httpContext) {
        await File.AppendAllTextAsync(Path, $"[{DateTime.Now} - {httpContext.Request.Path}] {exception.Message}\n");
    }

    public static async Task<string> Read() {
        return await File.ReadAllTextAsync(Path);
    }

    public static async Task Clear() {
        await File.WriteAllTextAsync(Path, "");
    }

    public static async Task Write(HttpContext httpContext, string bodyText) {
        Console.WriteLine(bodyText);
        var json = JsonConvert.DeserializeObject<ProblemDetails>(bodyText);
        await File.AppendAllTextAsync(Path,
            $"[{DateTime.Now} - {httpContext.Request.Path}] {json?.Title ?? "Generic Error"} - {json?.Detail ?? ""}\n");
    }
}