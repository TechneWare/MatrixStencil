namespace MatrixStencil.Core.Masks;

public sealed record MessageMaskBuilderOptions
{
    public int MarginColumns { get; init; } = 2;

    public int MarginRows { get; init; } = 2;

    public int GlyphSpacingPixels { get; init; } = 1;

    public int MaximumScale { get; init; } = 4;

    public int HorizontalStrokeExpansionColumns { get; init; } = 0;
}