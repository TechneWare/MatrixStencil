namespace MatrixStencil.Core.Animation;

public sealed record StencilOutlineAnimationOptions
{
    /// <summary>
    /// Time required for the reveal floor to rise from None through
    /// every Matrix intensity until it reaches Highlight.
    /// </summary>
    public double EqualizationDurationSeconds { get; init; } = 2.0;

    public double MinimumReleaseDelaySeconds { get; init; } = 1.0;

    public double MaximumReleaseDelaySeconds { get; init; } = 5.25;

    public double MinimumFallSpeedRowsPerSecond { get; init; } = 2.4;

    public double MaximumFallSpeedRowsPerSecond { get; init; } = 5.8;

    public double MorphToMatrixAfterRows { get; init; } = 2.0;

    public double HighlightDistanceRows { get; init; } = 2.5;

    public double BrightDistanceRows { get; init; } = 6.0;

    public double NormalDistanceRows { get; init; } = 11.0;

    public double MutedDistanceRows { get; init; } = 17.0;

    public int RenderPriority { get; init; } = 3;

    /// <summary>
    /// Edge cells use this restrained alphabet while attached to the
    /// stencil. They change into the normal Matrix alphabet after release.
    /// </summary>
    public string OutlineCharacters { get; init; } = "01|:#";
}