namespace MatrixStencil.Core.Simulation;

public sealed record MatrixLayerSnapshot(
    MatrixLayerKind Kind,
    IReadOnlyList<RainStream> Streams);
