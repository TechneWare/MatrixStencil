namespace MatrixStencil.Core.Simulation;

public sealed record MatrixLayerOptions
{
    public required MatrixLayerKind Kind { get; init; }

    public required double TargetStreamsPerColumn { get; init; }

    public required double SpawnRatePerColumnPerSecond { get; init; }

    public required double MinimumSpeedRowsPerSecond { get; init; }

    public required double MaximumSpeedRowsPerSecond { get; init; }

    public required int MinimumTrailLength { get; init; }

    public required int MaximumTrailLength { get; init; }

    public required int MinimumHighlightDelayRows { get; init; }

    public required int MaximumHighlightDelayRows { get; init; }

    public static MatrixLayerOptions CreateFar()
    {
        return new MatrixLayerOptions
        {
            Kind = MatrixLayerKind.Far,

            // Slightly denser so the distant layer fills dead space
            // without becoming the dominant layer.
            TargetStreamsPerColumn = 0.48,

            // Streams remain on screen much longer, so only a small
            // replacement rate is required.
            SpawnRatePerColumnPerSecond = 0.04,

            // The far layer should drift instead of falling at the same
            // speed as the foreground.
            MinimumSpeedRowsPerSecond = 0.70,
            MaximumSpeedRowsPerSecond = 1.20,

            // Longer trails make the distant background feel continuous.
            MinimumTrailLength = 14,
            MaximumTrailLength = 28,

            MinimumHighlightDelayRows = 0,
            MaximumHighlightDelayRows = 1
        };
    }

    public static MatrixLayerOptions CreateMiddle()
    {
        return new MatrixLayerOptions
        {
            Kind = MatrixLayerKind.Middle,
            TargetStreamsPerColumn = 0.46,
            SpawnRatePerColumnPerSecond = 0.10,
            MinimumSpeedRowsPerSecond = 2.8,
            MaximumSpeedRowsPerSecond = 5.0,
            MinimumTrailLength = 8,
            MaximumTrailLength = 20,
            MinimumHighlightDelayRows = 2,
            MaximumHighlightDelayRows = 8
        };
    }

    public static MatrixLayerOptions CreateForeground()
    {
        return new MatrixLayerOptions
        {
            Kind = MatrixLayerKind.Foreground,
            TargetStreamsPerColumn = 0.28,
            SpawnRatePerColumnPerSecond = 0.07,
            MinimumSpeedRowsPerSecond = 3.2,
            MaximumSpeedRowsPerSecond = 5.8,
            MinimumTrailLength = 7,
            MaximumTrailLength = 17,
            MinimumHighlightDelayRows = 4,
            MaximumHighlightDelayRows = 16
        };
    }
}