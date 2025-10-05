using System;
using FluentAssertions;
using Xunit;
public class TimeFormattingTests {
    [Theory]
    [InlineData(1690898960, "08/01/23 14:09:20")] // Test with UTC timezone
    public void Formats_like_required(long unix, string expected)
    {
        // Use UTC timezone to match the expected output
        var utc = DateTimeOffset.FromUnixTimeSeconds(unix);
        var s = utc.UtcDateTime.ToString("MM'/'dd'/'yy HH':'mm':'ss");
        s.Should().Be(expected);
    }
}
