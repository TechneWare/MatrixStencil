using MatrixStencil.Core.Randomness;

namespace MatrixStencil.Core.Simulation;

public sealed class MatrixLayer
{
    private readonly MatrixLayerOptions _options;
    private readonly IRandomSource _random;
    private readonly List<RainStream> _streams = [];
    private double _spawnBudget;

    public MatrixLayer(MatrixLayerOptions options, IRandomSource random)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public MatrixLayerKind Kind => _options.Kind;

    public MatrixLayerState State { get; private set; } = MatrixLayerState.Dormant;

    public bool IsAcceptingStreams => State is MatrixLayerState.Opening or MatrixLayerState.Active;

    public int ActiveStreamCount => _streams.Count;

    public long TotalSpawned { get; private set; }

    public IReadOnlyList<RainStream> Streams => _streams;

    public void Open()
    {
        if (State is MatrixLayerState.Opening or MatrixLayerState.Active)
        {
            return;
        }

        State = MatrixLayerState.Opening;
    }

    public void Close()
    {
        if (State == MatrixLayerState.Dormant)
        {
            return;
        }

        State = _streams.Count == 0
            ? MatrixLayerState.Dormant
            : MatrixLayerState.Closing;
    }

    public void Reset(int width, int height, bool prepopulate)
    {
        ValidateDimensions(width, height);

        _streams.Clear();
        _spawnBudget = 0;
        TotalSpawned = 0;
        State = MatrixLayerState.Dormant;

        if (!prepopulate)
        {
            return;
        }

        State = MatrixLayerState.Active;
        var target = GetTargetStreamCount(width);

        for (var index = 0; index < target; index++)
        {
            _streams.Add(CreateStream(width, height, prepopulate: true));
            TotalSpawned++;
        }
    }

    public void Update(TimeSpan elapsed, int width, int height)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        ValidateDimensions(width, height);

        foreach (var stream in _streams)
        {
            stream.Update(elapsed);
        }

        _streams.RemoveAll(stream => stream.HasExited(height));

        if (State == MatrixLayerState.Closing)
        {
            if (_streams.Count == 0)
            {
                State = MatrixLayerState.Dormant;
            }

            return;
        }

        if (!IsAcceptingStreams)
        {
            return;
        }

        var target = GetTargetStreamCount(width);
        var spawnRate = _options.SpawnRatePerColumnPerSecond * width;
        _spawnBudget += elapsed.TotalSeconds * spawnRate;

        while (_spawnBudget >= 1 && _streams.Count < target)
        {
            _streams.Add(CreateStream(width, height, prepopulate: false));
            _spawnBudget -= 1;
            TotalSpawned++;
        }

        if (_streams.Count >= Math.Max(1, (int)Math.Ceiling(target * 0.90)))
        {
            State = MatrixLayerState.Active;
        }
        else
        {
            State = MatrixLayerState.Opening;
        }
    }

    public MatrixLayerSnapshot CreateSnapshot()
    {
        return new MatrixLayerSnapshot(Kind, _streams.ToArray());
    }

    private RainStream CreateStream(int width, int height, bool prepopulate)
    {
        var trailLength = _random.Next(
            _options.MinimumTrailLength,
            _options.MaximumTrailLength + 1);

        var headRow = prepopulate
            ? _random.Next(-trailLength, height + trailLength)
            : -_random.Next(0, Math.Max(2, trailLength / 2));

        var speed =
            _options.MinimumSpeedRowsPerSecond +
            (_random.NextDouble() *
             (_options.MaximumSpeedRowsPerSecond - _options.MinimumSpeedRowsPerSecond));

        var highlightDelay = _random.Next(
            _options.MinimumHighlightDelayRows,
            _options.MaximumHighlightDelayRows + 1);

        return new RainStream(
            _options.Kind,
            _random.Next(0, width),
            headRow,
            speed,
            trailLength,
            highlightDelay,
            _random.Next(0, int.MaxValue));
    }

    private int GetTargetStreamCount(int width)
    {
        return Math.Max(1, (int)Math.Ceiling(width * _options.TargetStreamsPerColumn));
    }

    private static void ValidateDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }
    }
}
