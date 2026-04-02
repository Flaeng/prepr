namespace Prepr.Tests;

public class ProgressBarTests
{
    [Fact]
    public void Update_WritesProgressBarToWriter()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        bar.Update(50, "Testing...");

        var output = writer.ToString();
        Assert.Contains("50%", output);
        Assert.Contains("Testing...", output);
        Assert.Contains("\u2588", output); // filled block
        Assert.Contains("\u2591", output); // empty block
    }

    [Fact]
    public void Constructor_WhenTotalIsZero_Throws()
    {
        var writer = new StringWriter();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBar(writer, 0));
    }

    [Fact]
    public void Constructor_WhenTotalIsNegative_Throws()
    {
        var writer = new StringWriter();

        Assert.Throws<ArgumentOutOfRangeException>(() => new ProgressBar(writer, -1));
    }

    [Fact]
    public void Update_At100Percent_WritesNewline()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        bar.Update(100, "Done");

        var output = writer.ToString();
        Assert.Contains("100%", output);
        Assert.EndsWith(Environment.NewLine, output);
    }

    [Fact]
    public void Update_SamePercent_DoesNotWriteAgain()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        bar.Update(50, "Testing...");
        var firstOutput = writer.ToString();

        bar.Update(50, "Testing...");
        var secondOutput = writer.ToString();

        Assert.Equal(firstOutput, secondOutput);
    }

    [Fact]
    public void Complete_ClearsProgressLine()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        bar.Update(50, "Testing...");
        bar.Complete();

        var output = writer.ToString();
        // Should end with a carriage return clearing the line
        Assert.Contains("\r", output);
    }

    [Theory]
    [InlineData(0, 100, "0%")]
    [InlineData(1, 100, "1%")]
    [InlineData(25, 100, "25%")]
    [InlineData(50, 100, "50%")]
    [InlineData(75, 100, "75%")]
    [InlineData(99, 100, "99%")]
    [InlineData(100, 100, "100%")]
    public void Update_CalculatesPercentageCorrectly(int current, int total, string expectedPercent)
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, total);

        bar.Update(current, "phase");

        Assert.Contains(expectedPercent, writer.ToString());
    }

    [Theory]
    [InlineData(1, 3, "33%")]
    [InlineData(2, 3, "66%")]
    [InlineData(3, 3, "100%")]
    [InlineData(1, 7, "14%")]
    [InlineData(5, 7, "71%")]
    public void Update_TruncatesPercentageToInteger(int current, int total, string expectedPercent)
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, total);

        bar.Update(current, "phase");

        Assert.Contains(expectedPercent, writer.ToString());
    }

    [Fact]
    public void Update_CurrentExceedsTotal_ThrowsDueToNegativeBarWidth()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => bar.Update(150, "phase"));
    }

    [Fact]
    public void Update_FilledBlocksMatchPercentage()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        bar.Update(50, "phase");

        var output = writer.ToString();
        int filledCount = output.Count(c => c == '\u2588');
        int emptyCount = output.Count(c => c == '\u2591');

        Assert.Equal(15, filledCount); // 50% of 30 = 15
        Assert.Equal(15, emptyCount);
    }

    [Fact]
    public void Update_At0Percent_AllBlocksEmpty()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        bar.Update(0, "phase");

        var output = writer.ToString();
        int filledCount = output.Count(c => c == '\u2588');
        int emptyCount = output.Count(c => c == '\u2591');

        Assert.Equal(0, filledCount);
        Assert.Equal(30, emptyCount);
    }

    [Fact]
    public void Update_At100Percent_AllBlocksFilled()
    {
        var writer = new StringWriter();
        var bar = new ProgressBar(writer, 100);

        bar.Update(100, "phase");

        var output = writer.ToString();
        int filledCount = output.Count(c => c == '\u2588');
        int emptyCount = output.Count(c => c == '\u2591');

        Assert.Equal(30, filledCount);
        Assert.Equal(0, emptyCount);
    }
}
