using MatrixStencil.Core.Animation;

namespace MatrixStencil.Core.Simulation;

public sealed record MatrixSimulationOptions
{
    public HeatCycleOptions HeatCycle { get; init; } = HeatCycleOptions.Default;

    public MatrixLayerOptions FarLayer { get; init; } = MatrixLayerOptions.CreateFar();

    public MatrixLayerOptions MiddleLayer { get; init; } = MatrixLayerOptions.CreateMiddle();

    public MatrixLayerOptions ForegroundLayer { get; init; } = MatrixLayerOptions.CreateForeground();
}
