namespace MatrixStencil.Core.Animation;

public sealed record StencilOutlineAnimationOptions
{
    public double MinimumActivationDelaySeconds { get; init; } = 0.0;

    public double MaximumActivationDelaySeconds { get; init; } = 0.85;

    public double ChargeDurationSeconds { get; init; } = 2.10;

    public double MinimumReleaseDelaySeconds { get; init; } = 3.0;

    public double MaximumReleaseDelaySeconds { get; init; } = 5.25;

    public double MinimumFallSpeedRowsPerSecond { get; init; } = 3.4;

    public double MaximumFallSpeedRowsPerSecond { get; init; } = 5.8;

    public double MorphToMatrixAfterRows { get; init; } = 2.5;

    public double HighlightDistanceRows { get; init; } = 2.5;

    public double BrightDistanceRows { get; init; } = 6.0;

    public double NormalDistanceRows { get; init; } = 11.0;

    public double MutedDistanceRows { get; init; } = 17.0;

    public int RenderPriority { get; init; } = 3;

    public string OutlineCharacters { get; init; } = "01|:#";
}