namespace MatrixStencil.Core.Rendering;

public sealed class StencilMapper
{
    private const MatrixIntensity StencilInteriorIntensity =
        MatrixIntensity.DeepShadow;

    private const MatrixIntensity ExteriorEdgeIntensity =
        MatrixIntensity.Bright;

    private const MatrixIntensity StencilEdgeIntensity =
        MatrixIntensity.Highlight;

    public MatrixIntensity Map(
        MatrixIntensity intensity,
        bool insideStencil,
        bool onStencilEdge,
        bool adjacentToStencilEdge,
        bool peakRevealActive,
        bool stencilEdgeHighlightsEnabled = true)
    {
        if (intensity == MatrixIntensity.None)
        {
            return MatrixIntensity.None;
        }

        if (stencilEdgeHighlightsEnabled)
        {
            if (onStencilEdge)
            {
                return StencilEdgeIntensity;
            }

            if (adjacentToStencilEdge)
            {
                return intensity <
                       ExteriorEdgeIntensity
                    ? ExteriorEdgeIntensity
                    : intensity;
            }
        }

        if (!insideStencil)
        {
            return intensity;
        }

        return peakRevealActive
            ? DemoteOneLevel(intensity)
            : StencilInteriorIntensity;
    }

    private static MatrixIntensity DemoteOneLevel(
        MatrixIntensity intensity)
    {
        var demotedValue =
            Math.Max(
                (int)MatrixIntensity.DeepShadow,
                (int)intensity - 1);

        return (MatrixIntensity)demotedValue;
    }
}