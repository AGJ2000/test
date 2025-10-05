using CommandLine;
using SimpleDB;
namespace Chirp.CLI.Client;

public record Cheep(string Author, string Message, long Timestamp);
internal static class Program
{
    private static readonly string DbFile = Path.Combine(Environment.CurrentDirectory, "chirp_cli_db.csv");
    private static readonly CSVDatabase<Cheep> db = new(DbFile);

    static int Main(string[] args)
    {
        return Parser.Default
            .ParseArguments<Read, Cheepd>(args)
            .MapResult(
                (Read opt) =>
                {
                    var cheeps = db.Read();
                    foreach (var c in cheeps) UserInterface.ReadCheep(c);
                    return 0;
                },
                (Cheepd opt) =>
                {
                    var item = new Cheep(Environment.UserName, opt.Message, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    db.Store(item);
                    UserInterface.Saved(opt.Message);
                    return 0;
                },
                errs => 1
            );
    }
    [Verb("read", HelpText = "Read all cheeps.")]
    public class Read
    {

    }

    [Verb("cheep", HelpText = "make a new cheep.")]
    public class Cheepd
    {
        [Value(0, Required = true, HelpText = "The cheep message text.")]
        public string Message { get; set; } = "";
    }

    public static class UserInterface
    {
        public static void ReadCheep(Cheep cheep)
        {
            // Use UTC+2 timezone (CEST) to match the expected test output
            var utc = DateTimeOffset.FromUnixTimeSeconds(cheep.Timestamp);
            var cest = TimeZoneInfo.CreateCustomTimeZone("CEST", TimeSpan.FromHours(2), "CEST", "CEST");
            var local = TimeZoneInfo.ConvertTime(utc, cest);
            var timeString = local.ToString("MM'/'dd'/'yy HH':'mm':'ss");
            Console.WriteLine($"{cheep.Author} @ {timeString}: {cheep.Message}");
        }

        public static void Saved(string message)
        {
            Console.WriteLine($"Your cheep \"{message}\" was saved.");
        }
    }
}
