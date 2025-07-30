namespace HandballBackend.EndpointHelpers;

public static class GitHelper {
    private static Timer _timer;

    private static void CheckForUpdates() {
        RunGitCommand("fetch --all");
        var localHash = RunGitCommand("rev-parse master");
        var newHash = RunGitCommand("rev-parse origin/master");

        if (localHash == newHash) return;
        Console.WriteLine("Updates on master found; restarting ");
        System.Diagnostics.Process.Start("..\\download-latest.cmd");
        Environment.Exit(0);
    }

    private static string RunGitCommand(string arguments) {
        var process = new System.Diagnostics.Process {
            StartInfo = new System.Diagnostics.ProcessStartInfo {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output.Trim();
    }

    public static void StartCheckingForUpdates() {
        CheckForUpdates();
        _timer = new Timer(_ => CheckForUpdates(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }
}