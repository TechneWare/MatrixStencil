namespace MatrixStencil.Core.Rendering;

public readonly record struct StencilImpact(
    int X,
    int Y,
    MatrixIntensity Intensity);