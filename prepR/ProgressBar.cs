namespace PrepR;

public sealed class ProgressBar
{
    private const int BarWidth = 30;
    private readonly TextWriter _writer;
    private readonly int _total;
    private int _lastPercent = -1;

    public ProgressBar(TextWriter writer, int total)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);
        _writer = writer;
        _total = total;
    }

    public void Update(int current, string phase)
    {
        int percent = (int)((double)current / _total * 100);
        if (percent <= _lastPercent)
            return;

        _lastPercent = percent;

        int filled = (int)((double)percent / 100 * BarWidth);
        int empty = BarWidth - filled;

        var line = $"\r[{new string('\u2588', filled)}{new string('\u2591', empty)}] {percent,3}% \u2014 {phase}";
        _writer.Write(line.PadRight(BarWidth + 60));

        if (percent >= 100)
            _writer.WriteLine();
    }

    public void Complete()
    {
        // Clear the progress line
        _writer.Write($"\r{new string(' ', BarWidth + 60)}\r");
    }
}
