namespace HandballBackend.EndpointHelpers;

public static class GitHelper {
    private static string? _hash = null;
    private static Timer _timer;

    private static void CheckForUpdates() {
        Console.WriteLine("Begin checking for updates...");
        var process = new System.Diagnostics.Process();

        var startInfo = new System.Diagnostics.ProcessStartInfo {
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            FileName = "cmd.exe",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.StartInfo = startInfo;
        process.Start();
        using var sw = process.StandardInput;
        if (sw.BaseStream.CanWrite) {
            sw.WriteLine("git fetch --all");
            sw.Flush();
            sw.WriteLine("git rev-parse origin/master");
            sw.Flush();
        }

        using var output = process.StandardOutput;
        var head = output.ReadToEnd();
        Console.WriteLine(head);
        _hash ??= head;

        if (_hash == head) return;
        Console.WriteLine("Updates on master found; restarting ");
        sw.WriteLine("start download-latest.cmd");
        sw.Flush();
        Environment.Exit(0);
    }

    public static void StartCheckingForUpdates() {
        _timer = new Timer(_ => CheckForUpdates(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }
}