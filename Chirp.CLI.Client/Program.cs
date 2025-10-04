using System;
using System.Globalization;
using System.IO;
using System.Text;

static class Program
{
    // Path to the CSV "database" (created on first write)
    private const string DbFile = "chirp_cli_db.csv";

    // Entry point: support `read` and `cheep "message"`
    public static int Main(string[] args)
    {
        // New-style Program.cs is fine; we keep it old-school for clarity.
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var cmd = args[0].Trim().ToLowerInvariant();
        try
        {
            return cmd switch
            {
                "read" => CmdRead(),
                "cheep" => CmdCheep(args),
                _ => Unknown(cmd)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
    }

    private static int Unknown(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Chirp.CLI read");
        Console.WriteLine("  Chirp.CLI cheep \"Hello, world!\"");
        Console.WriteLine();
        Console.WriteLine("Or via dotnet:");
        Console.WriteLine("  dotnet run -- read");
        Console.WriteLine("  dotnet run -- cheep \"Hello, world!\"");
    }

    private static int CmdRead()
    {
        if (!File.Exists(DbFile))
            return 0; // Nothing to show; empty DB is fine.

        // Read all lines and print them formatted
        foreach (var line in File.ReadLines(DbFile, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = ParseCsvLine(line);
            if (fields.Length < 3) continue;

            var author = fields[0];
            var tsRaw = fields[1];
            var msg = fields[2];

            if (!long.TryParse(tsRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
                continue;

            var local = DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().DateTime;

            // Required display format: "name @ 08/01/23 14:09:20: message"
            // US-style date to match the example (MM/dd/yy HH:mm:ss)
            var when = local.ToString("MM'/'dd'/'yy HH':'mm':'ss", CultureInfo.InvariantCulture);
            Console.WriteLine($"{author} @ {when}: {msg}");
        }
        return 0;
    }

    private static int CmdCheep(string[] args)
    {
        // Expect: cheep "message..."
        if (args.Length < 2)
            throw new ArgumentException("Missing cheep message. Example: Chirp.CLI cheep \"Hello, world!\"");

        // Join remaining args to allow spaces without quotes, but quotes are recommended.
        var message = string.Join(' ', args[1..]).Trim();
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message must not be empty.");

        // Author from OS user, timestamp as Unix seconds (UTC)
        var author = Environment.UserName;
        var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Append CSV line: author,unix_ts,message
        // CSV escaping: wrap fields in quotes, double any inner quotes.
        var line = string.Join(',',
            CsvEscape(author),
            CsvEscape(unix.ToString(CultureInfo.InvariantCulture)),
            CsvEscape(message)
        );

        // Ensure file exists; then append line
        using (var sw = new StreamWriter(DbFile, append: true, Encoding.UTF8))
        {
            sw.WriteLine(line);
        }

        return 0;
    }

    // Very small CSV helper: returns fields for a single line.
    // Supports standard quotes: "field,with,comma" and inner quotes doubled: "".
    private static string[] ParseCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    // Double quote inside a quoted field -> literal quote
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false; // end of quoted field
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }

    private static string CsvEscape(string value)
    {
        // Always quote. Double any existing quotes.
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
