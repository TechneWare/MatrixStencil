namespace MatrixStencil.Core.Rendering;

public readonly record struct MatrixCell(
    char Character,
    MatrixIntensity Intensity,
    int LayerPriority)
{
    public static MatrixCell Empty { get; } = new(' ', MatrixIntensity.None, -1);
}
