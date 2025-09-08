namespace HandballBackend.Utils;

public class RequestLogger {
    private readonly RequestDelegate _next;

    public RequestLogger(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context) {
        // Ensure the request body can be read multiple times
        context.Request.EnableBuffering();

        // Create a stream reader to read the request body
        using (StreamReader reader = new(context.Request.Body, leaveOpen: true)) {
            string body = await reader.ReadToEndAsync();

            // Reset the request body stream position so the next middleware can read it
            context.Request.Body.Position = 0;

            Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
            // Console.WriteLine($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");

            if (!string.IsNullOrWhiteSpace(body)) {
                Console.WriteLine($"\tBody: {body}");
            }
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }
}