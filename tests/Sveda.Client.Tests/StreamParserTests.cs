using Xunit;

namespace Sveda.Client.Tests;

public sealed class StreamParserTests
{
    [Fact]
    public void ItParsesStreamEventsAndStopsOnDone()
    {
        var content = string.Join("\n",
        [
            ": connected",
            "",
            "data: {\"type\":\"message.start\"}",
            "",
            "data: {\"type\":\"text.delta\",\"delta\":\"Hello\"}",
            "",
            "data: {\"type\":\"message.end\",\"finishReason\":\"stop\"}",
            "",
            "data: [DONE]",
            "",
        ]);

        var events = new StreamParser().Iterate(content).ToList();

        Assert.Equal(3, events.Count);
        Assert.Equal("message.start", events[0].Type);
        Assert.Equal("text.delta", events[1].Type);
        Assert.Equal("Hello", events[1]["delta"]);
        Assert.Equal("message.end", events[2].Type);
    }

    [Fact]
    public void ItIgnoresInvalidLines()
    {
        var events = new StreamParser().Iterate("event: ping\ndata: not-json\ndata: {\"type\":\"unknown.event\"}\n").ToList();

        Assert.Empty(events);
    }
}
