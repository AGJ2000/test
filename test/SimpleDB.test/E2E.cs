namespace Chirp.CLI.test.SimpleDB.test;

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

public class E2E
{
    string DotnetExe => "dotnet";
    string CliProject => Path.GetFullPath(Path.Combine("..","..","..","..","src","Chirp.CLI.Client","Chirp.CLI.Client.csproj"));

    [Fact]
    public void Cheep_appends_and_read_shows_it()
    {
        using var dir = new TempDir();
        var db = Path.Combine(dir.Path, "chirp_cli_db.csv");

        Run("run -- cheep \"Hello!!!\"", dir.Path, out _).Should().Be(0);
        File.Exists(db).Should().BeTrue();

        var exit = Run("run -- read", dir.Path, out var output);
        exit.Should().Be(0);
        output.Should().Contain("Hello!!!");
        output.Should().Contain(Environment.UserName);
    }

    [Fact]
    public void Read_prints_expected_format_for_seed_data()
    {
        using var dir = new TempDir();
        var db = Path.Combine(dir.Path, "chirp_cli_db.csv");
        File.WriteAllText(db, string.Join(Environment.NewLine, new[]{
            "Author,Timestamp,Message",
            "\"ropf\",\"1690898960\",\"Hello, BDSA students!\"",
            "\"rnie\",\"1690982378\",\"Welcome to the course!\"",
            "\"rnie\",\"1690983458\",\"I hope you had a good summer.\"",
            "\"ropf\",\"1690985087\",\"Cheeping cheeps on Chirp :)\""
        }));

        Run("run -- read", dir.Path, out var output).Should().Be(0);
        output.Should().Contain("ropf @ 08/01/23 14:09:20: Hello, BDSA students!");
        output.Should().Contain("rnie @ 08/02/23 14:19:38: Welcome to the course!");
        output.Should().Contain("rnie @ 08/02/23 14:37:38: I hope you had a good summer.");
        output.Should().Contain("ropf @ 08/02/23 15:04:47: Cheeping cheeps on Chirp :)");
    }

    int Run(string args, string workDir, out string stdOut)
    {
        var psi = new ProcessStartInfo(DotnetExe, $"--project \"{CliProject}\" {args}")
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = Process.Start(psi)!;
        stdOut = p.StandardOutput.ReadToEnd();
        var err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0) Console.WriteLine(err);
        return p.ExitCode;
    }

    sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        public TempDir() { Directory.CreateDirectory(Path); }
        public void Dispose() { try { Directory.Delete(Path, true); } catch { } }
    }
}
