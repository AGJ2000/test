using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

public class End2End
{
    // Brug samme config lokalt/CI: set TEST_BUILD_CONFIG=Release i CI. Lokalt falder den tilbage til Debug.
    static string BuildConfig => Environment.GetEnvironmentVariable("TEST_BUILD_CONFIG") ?? "Debug";

    static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Chirp.sln")))
            dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("Could not locate Chirp.sln upward from test bin directory.");
        return dir.FullName;
    }

    static string CliProjectPath =>
        Path.Combine(SolutionRoot(), "src", "Chirp.CLI.Client", "Chirp.CLI.Client.csproj");

    static int RunCli(string appArgs, string workDir, out string stdout, out string stderr, bool noBuild = true)
    {
        var psi = new ProcessStartInfo(
            "dotnet",
            $"run --project \"{CliProjectPath}\" -c {BuildConfig} {(noBuild ? "--no-build" : "")} -- {appArgs}"
        )
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };
        using var p = Process.Start(psi)!;
        stdout = p.StandardOutput.ReadToEnd();
        stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode;
    }

    [Fact]
    public void TestReadCheep()
    {
        using var tmp = new TempDir();
        ArrangeTestDatabase(tmp.Path); // skriver CSV MED header

        // Kald CLI korrekt: din app har 'read', ikke 'read 10'
        var exit = RunCli("read", tmp.Path, out var output, out var err);
        if (exit != 0) throw new Exception($"CLI failed: {err}");

        var first = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        Assert.StartsWith("ropf @ ", first);           // forfatter i starten
        Assert.EndsWith(": Hello, World!", first);     // besked i slutningen
    }

    private static void ArrangeTestDatabase(string workDir)
    {
        // Din CSVDatabase er sat til HasHeaderRecord=true → skriv header
        // Din Cheep er (Author, Message, Timestamp) → skriv i samme rækkefølge
        var db = Path.Combine(workDir, "chirp_cli_db.csv");
        var content = string.Join(Environment.NewLine, new[]{
            "Author,Message,Timestamp",
            "ropf,\"Hello, World!\",1640995200"
        }) + Environment.NewLine;
        File.WriteAllText(db, content);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chirp-e2e-" + Guid.NewGuid().ToString("N"));
        public TempDir() => Directory.CreateDirectory(Path);
        public void Dispose() { try { Directory.Delete(Path, true); } catch { } }
    }
}
