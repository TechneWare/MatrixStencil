namespace MatrixStencil.Core.Simulation;

public sealed class RainStream
{
    public RainStream(
        MatrixLayerKind layerKind,
        int column,
        double headRow,
        double speedRowsPerSecond,
        int trailLength,
        int highlightDelayRows,
        int seed)
    {
        if (column < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        if (speedRowsPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speedRowsPerSecond));
        }

        if (trailLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trailLength));
        }

        if (highlightDelayRows < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(highlightDelayRows));
        }

        LayerKind = layerKind;
        Column = column;
        BirthRow = headRow;
        HeadRow = headRow;
        SpeedRowsPerSecond = speedRowsPerSecond;
        TrailLength = trailLength;
        HighlightDelayRows = highlightDelayRows;
        Seed = seed;
    }

    public MatrixLayerKind LayerKind { get; }

    public int Column { get; }

    public double BirthRow { get; }

    public double HeadRow { get; private set; }

    public double SpeedRowsPerSecond { get; }

    public int TrailLength { get; }

    public int HighlightDelayRows { get; }

    public int Seed { get; }

    public double DistanceTraveled { get; private set; }

    public double VisibleRowsTraveled => Math.Max(0, HeadRow);

    public bool IsHighlightMature => VisibleRowsTraveled >= HighlightDelayRows;

    public void Update(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        var distance = SpeedRowsPerSecond * elapsed.TotalSeconds;
        HeadRow += distance;
        DistanceTraveled += distance;
    }

    public bool HasExited(int screenHeight)
    {
        return HeadRow - TrailLength >= screenHeight;
    }
}
