namespace MatrixStencil.Core.Animation;

public sealed record HeatCycleOptions
{
    public static HeatCycleOptions Default { get; } = new();

    public TimeSpan ColdHoldDuration { get; init; } =
        TimeSpan.FromSeconds(2);

    public TimeSpan MiddleWarmupDuration { get; init; } =
        TimeSpan.FromSeconds(5);

    public TimeSpan ForegroundWarmupDuration { get; init; } =
        TimeSpan.FromSeconds(4);

    public TimeSpan HotHoldDuration { get; init; } =
        TimeSpan.FromSeconds(3);

    public TimeSpan PeakRevealDuration { get; init; } =
        TimeSpan.FromSeconds(4);
}