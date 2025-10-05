using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
namespace SimpleDB.test;
public sealed class End2End
{
    // Brug samme build-config som CI. Lokalt falder den tilbage til Debug.
    private static string BuildConfig =>
        Environment.GetEnvironmentVariable("TEST_BUILD_CONFIG") ?? "Debug";

    // Find solution-roden ved at gå opad til vi finder Chirp.sln
    private static string SolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Chirp.sln")))
            dir = dir.Parent;
        if (dir == null) throw new InvalidOperationException("Could not locate Chirp.sln");
        return dir.FullName;
    }

    // Absolut sti til CLI-projektet (som din repo hedder)
    private static string CliProjectPath =>
        Path.Combine(SolutionRoot(), "src", "Chirp.CLI.Client", "Chirp.CLI.Client.csproj");

    private static readonly string[] separator = ["\r\n", "\n"];

    // Helper til at køre CLI'en via dotnet run
    private static int RunCli(string appArgs, string workDir, out string stdout, out string stderr, bool noBuild = true)
    {
        var psi = new ProcessStartInfo(
            "dotnet",
            $"run --project \"{CliProjectPath}\" -c {BuildConfig} {(noBuild ? "--no-build" : "")} -- {appArgs}"
        )
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var p = Process.Start(psi)!;
        stdout = p.StandardOutput.ReadToEnd();
        stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode;
    }

    [Fact]
    public void Read_prints_expected_content_for_seed_data()
    {
        using var dir = new TempDir();
        ArrangeTestDatabaseWithHeader(dir.Path);

        var exit = RunCli("read", dir.Path, out var output, out var err);
        if (exit != 0) throw new Exception($"CLI failed (exit {exit}):\n{err}");

        // Vi tester robust: forfatter + besked, ikke tidszone-afhængig tidstreng
        var lines = output.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains(lines, l => l.Contains("ropf @ ") && l.EndsWith(": Hello, BDSA students!"));
        Assert.Contains(lines, l => l.Contains("rnie @ ") && l.EndsWith(": Welcome to the course!"));
        Assert.Contains(lines, l => l.Contains("rnie @ ") && l.EndsWith(": I hope you had a good summer."));
        Assert.Contains(lines, l => l.Contains("ropf @ ") && l.EndsWith(": Cheeping cheeps on Chirp :)"));
    }

    [Fact]
    public void Cheep_appends_and_read_shows_it()
    {
        using var dir = new TempDir();

        var exit = RunCli("cheep \"Hello!!!\"", dir.Path, out _, out var err1);
        if (exit != 0) throw new Exception($"CLI failed (exit {exit}) on cheep:\n{err1}");

        exit = RunCli("read", dir.Path, out var output, out var err2);
        if (exit != 0) throw new Exception($"CLI failed (exit {exit}) on read:\n{err2}");

        Assert.Contains("Hello!!!", output);
        Assert.Contains(Environment.UserName, output);
    }

    // Skriv CSV MED header, så det matcher CSVDatabase konfigurationen (HasHeaderRecord = true)
    private static void ArrangeTestDatabaseWithHeader(string workDir)
    {
        var db = Path.Combine(workDir, "chirp_cli_db.csv");
        var lines = new[]
        {
            "Author,Message,Timestamp",
            "ropf,\"Hello, BDSA students!\",1690898960",
            "rnie,\"Welcome to the course!\",1690982378",
            "rnie,\"I hope you had a good summer.\",1690983458",
            "ropf,\"Cheeping cheeps on Chirp :)\",1690985087"
        };
        File.WriteAllText(db, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    // Midlertidig mappe til hver test, så vi ikke rører rigtige filer
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chirp-e2e-" + Guid.NewGuid().ToString("N"));
        public TempDir() => Directory.CreateDirectory(Path);
        public void Dispose() { try { Directory.Delete(Path, recursive: true); } catch { } }
    }
}
