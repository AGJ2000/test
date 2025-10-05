using System;
using FluentAssertions;
using Xunit;

public class TimeFormattingTests
{
    [Theory]
    [InlineData(1690898960, "08/01/23 14:09:20")] // Juster hvis din lokale TZ giver andet
    public void Formats_like_required(long unix, string expected)
    {
        var local = DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime();
        var s = local.ToString("MM'/'dd'/'yy HH':'mm':'ss");
        s.Should().Be(expected);
    }
}
