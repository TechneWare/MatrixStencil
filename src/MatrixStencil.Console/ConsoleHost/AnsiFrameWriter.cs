using System.Text;
using MatrixStencil.Core.Rendering;

namespace MatrixStencil.ConsoleHost;

internal sealed class AnsiFrameWriter : IDisposable
{
    private const string Escape = "\u001b[";

    private MatrixFrame? _previousFrame;
    private MatrixIntensity? _currentIntensity;
    private bool _disposed;

    public AnsiFrameWriter()
    {
        if (!AnsiSupport.TryEnable())
        {
            throw new InvalidOperationException(
                "This terminal does not support ANSI " +
                "virtual-terminal output.");
        }

        Console.OutputEncoding = Encoding.UTF8;

        Console.Write(
            $"{Escape}2J" +
            $"{Escape}H" +
            $"{Escape}?25l");
    }

    public void Write(MatrixFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (_previousFrame is null ||
            _previousFrame.Width != frame.Width ||
            _previousFrame.Height != frame.Height)
        {
            WriteFullFrame(frame);
        }
        else
        {
            WriteChangedCells(
                frame,
                _previousFrame);
        }

        _previousFrame = frame;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Console.Write(
            $"{Escape}0m" +
            $"{Escape}?25h" +
            $"{Escape}2J" +
            $"{Escape}H");
    }

    private void WriteFullFrame(MatrixFrame frame)
    {
        var builder = new StringBuilder(
            (frame.Width + 20) * frame.Height);

        builder
            .Append(Escape)
            .Append("2J")
            .Append(Escape)
            .Append('H');

        _currentIntensity = null;

        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                AppendCell(
                    builder,
                    frame[x, y]);
            }

            if (y < frame.Height - 1)
            {
                builder.Append("\r\n");
            }
        }

        Console.Write(builder.ToString());
    }

    private void WriteChangedCells(
        MatrixFrame frame,
        MatrixFrame previousFrame)
    {
        var builder = new StringBuilder();

        for (var y = 0; y < frame.Height; y++)
        {
            var x = 0;

            while (x < frame.Width)
            {
                while (x < frame.Width &&
                       frame[x, y] == previousFrame[x, y])
                {
                    x++;
                }

                if (x >= frame.Width)
                {
                    break;
                }

                AppendCursorPosition(
                    builder,
                    x,
                    y);

                while (x < frame.Width &&
                       frame[x, y] != previousFrame[x, y])
                {
                    AppendCell(
                        builder,
                        frame[x, y]);

                    x++;
                }
            }
        }

        if (builder.Length > 0)
        {
            Console.Write(builder.ToString());
        }
    }

    private void AppendCell(
        StringBuilder builder,
        MatrixCell cell)
    {
        if (cell.Intensity != _currentIntensity)
        {
            AppendColor(
                builder,
                cell.Intensity);

            _currentIntensity = cell.Intensity;
        }

        builder.Append(cell.Character);
    }

    private static void AppendCursorPosition(
        StringBuilder builder,
        int x,
        int y)
    {
        builder
            .Append(Escape)
            .Append(y + 1)
            .Append(';')
            .Append(x + 1)
            .Append('H');
    }

    private static void AppendColor(
        StringBuilder builder,
        MatrixIntensity intensity)
    {
        var color = ConsolePalette.Get(intensity);

        builder
            .Append(Escape)
            .Append("38;2;")
            .Append(color.Red)
            .Append(';')
            .Append(color.Green)
            .Append(';')
            .Append(color.Blue)
            .Append('m');
    }
}