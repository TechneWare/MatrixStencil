using System.Diagnostics;
using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Rendering;
using MatrixStencil.Core.Simulation;
using MatrixStencil.Core.Animation;

namespace MatrixStencil.ConsoleHost;

internal sealed class MatrixConsoleApplication : IDisposable
{
    private const int FramesPerSecond = 30;
    private readonly string _message;
    private readonly AnsiFrameWriter _writer;
    private readonly MessageMaskBuilder _maskBuilder = new();
    private readonly FrameRenderer _renderer = new();
    private readonly StencilOutlineAnimation _outlineAnimation = new();
    private bool _disposed;
    private bool _paused;
    private bool _stopRequested;

    public MatrixConsoleApplication(string message)
    {
        _message = message;
        _writer = new AnsiFrameWriter();

        try
        {
            Console.Title = $"Matrix Stencil: {message} | R restart | Space pause | Q quit";
        }
        catch (IOException)
        {
            // Some Linux terminals do not expose a writable console title.
        }

        Console.CancelKeyPress += HandleCancelKeyPress;
    }

    public int Run()
    {
        var (width, height) = GetConsoleDimensions();
        var simulation = new MatrixSimulation(width, height, Environment.TickCount);
        var mask = _maskBuilder.Build(_message, width, height);
        var frameNumber = 0;

        var clock = Stopwatch.StartNew();
        var previous = clock.Elapsed;
        var nextFrame = previous;
        var targetFrameDuration = TimeSpan.FromSeconds(1.0 / FramesPerSecond);

        while (!_stopRequested)
        {
            HandleKeyboard(simulation);

            var currentDimensions = GetConsoleDimensions();

            if (currentDimensions.Width != width || currentDimensions.Height != height)
            {
                width = currentDimensions.Width;
                height = currentDimensions.Height;
                simulation.Resize(width, height);
                mask = _maskBuilder.Build(_message, width, height);
                _outlineAnimation.Reset();
                previous = clock.Elapsed;
            }

            var now = clock.Elapsed;
            var elapsed = now - previous;
            previous = now;

            if (!_paused)
            {
                var frameElapsed =
                    Min(
                        elapsed,
                        TimeSpan.FromMilliseconds(100));

                simulation.Update(frameElapsed);

                _outlineAnimation.Update(
                    simulation.Phase,
                    mask,
                    frameElapsed);

                frameNumber++;
            }

            var frame = _renderer.Render(
                width,
                height,
                simulation.CreateSnapshots(),
                mask,
                frameNumber,
                peakRevealActive:
                    simulation.IsPeakReveal,
                stencilEdgeHighlightsEnabled:
                    !_outlineAnimation.SuppressStencilEdgeHighlights,
                impactSink:
                    _outlineAnimation);

            _outlineAnimation.Render(frame);

            _writer.Write(frame);

            nextFrame += targetFrameDuration;
            var delay = nextFrame - clock.Elapsed;

            if (delay > TimeSpan.Zero)
            {
                Thread.Sleep(delay);
            }
            else
            {
                nextFrame = clock.Elapsed;
            }
        }

        return 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Console.CancelKeyPress -= HandleCancelKeyPress;
        _writer.Dispose();
    }

    private void HandleKeyboard(MatrixSimulation simulation)
    {
        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey(intercept: true).Key)
            {
                case ConsoleKey.R:
                    simulation.Restart();
                    _outlineAnimation.Reset();
                    break;

                case ConsoleKey.Spacebar:
                    _paused = !_paused;
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    _stopRequested = true;
                    break;
            }
        }
    }

    private void HandleCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
    {
        eventArgs.Cancel = true;
        _stopRequested = true;
    }

    private static (int Width, int Height) GetConsoleDimensions()
    {
        var width = Math.Max(20, Console.WindowWidth - 1);
        var height = Math.Max(10, Console.WindowHeight - 1);
        return (width, height);
    }

    private static TimeSpan Min(TimeSpan left, TimeSpan right)
    {
        return left <= right ? left : right;
    }
}
