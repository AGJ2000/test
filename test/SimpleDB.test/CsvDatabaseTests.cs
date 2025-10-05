using SimpleDB;

namespace Chirp.CLI.test.SimpleDB.test;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using SimpleDB;
using Xunit;

public record Cheep(string Author, string Message, long Timestamp);

public class CsvDatabaseTests
{
    [Fact]
    public void Store_then_Read_returns_item()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        try
        {
            var db = CSVDatabase<Cheep>.Instance(tmp); // singleton i 2.b
            var c = new Cheep("tester", "hello", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            db.Store(c);

            var items = db.Read().ToList();
            items.Should().ContainSingle();
            items[0].Should().Be(c);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
}
