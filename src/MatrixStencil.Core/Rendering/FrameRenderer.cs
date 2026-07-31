using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Rendering;

public sealed class FrameRenderer
{
    private readonly StencilMapper _stencilMapper;

    public FrameRenderer(
        StencilMapper? stencilMapper = null)
    {
        _stencilMapper =
            stencilMapper ??
            new StencilMapper();
    }

    public MatrixFrame Render(
        int width,
        int height,
        IReadOnlyList<MatrixLayerSnapshot> layers,
        MessageMask mask,
        int frameNumber,
        bool peakRevealActive = false,
        bool stencilEdgeHighlightsEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(layers);
        ArgumentNullException.ThrowIfNull(mask);

        if (mask.Width != width ||
            mask.Height != height)
        {
            throw new ArgumentException(
                "The message mask dimensions must match " +
                "the rendered frame.",
                nameof(mask));
        }

        var frame =
            new MatrixFrame(width, height);

        foreach (var layer in layers)
        {
            var priority =
                GetLayerPriority(layer.Kind);

            foreach (var stream in layer.Streams)
            {
                RenderStream(
                    frame,
                    mask,
                    stream,
                    priority,
                    frameNumber,
                    peakRevealActive,
                    stencilEdgeHighlightsEnabled);
            }
        }

        return frame;
    }

    private void RenderStream(
        MatrixFrame frame,
        MessageMask mask,
        RainStream stream,
        int priority,
        int frameNumber,
        bool peakRevealActive,
        bool stencilEdgeHighlightsEnabled)
    {
        var baseHeadRow =
            (int)Math.Floor(stream.HeadRow);

        var fractionalRow =
            stream.HeadRow - baseHeadRow;

        for (var trailIndex = 0;
             trailIndex < stream.TrailLength;
             trailIndex++)
        {
            var baseY =
                baseHeadRow - trailIndex;

            var baseIntensity =
                GetIntensity(
                    stream,
                    trailIndex);

            var character =
                MatrixCharacterGenerator.Pick(
                    stream.Seed,
                    frameNumber,
                    trailIndex,
                    stream.LayerKind);

            RenderCharacterSample(
                frame,
                mask,
                stream.LayerKind,
                stream.Column,
                baseY,
                character,
                baseIntensity,
                priority,
                1.0 - fractionalRow,
                peakRevealActive,
                stencilEdgeHighlightsEnabled);

            RenderCharacterSample(
                frame,
                mask,
                stream.LayerKind,
                stream.Column,
                baseY + 1,
                character,
                baseIntensity,
                priority,
                fractionalRow,
                peakRevealActive,
                stencilEdgeHighlightsEnabled);
        }
    }

    private void RenderCharacterSample(
        MatrixFrame frame,
        MessageMask mask,
        MatrixLayerKind layerKind,
        int x,
        int y,
        char character,
        MatrixIntensity baseIntensity,
        int priority,
        double weight,
        bool peakRevealActive,
        bool stencilEdgeHighlightsEnabled)
    {
        if (x < 0 ||
            x >= frame.Width ||
            y < 0 ||
            y >= frame.Height)
        {
            return;
        }

        var intensity =
            ScaleIntensity(
                baseIntensity,
                weight,
                preserveLowIntensity:
                    layerKind == MatrixLayerKind.Far);

        if (intensity == MatrixIntensity.None)
        {
            return;
        }

        intensity = _stencilMapper.Map(
            intensity,
            insideStencil:
                mask.Contains(x, y),
            onStencilEdge:
                mask.IsEdge(x, y),
            adjacentToStencilEdge:
                mask.IsAdjacentToEdge(x, y),
            peakRevealActive:
                peakRevealActive,
            stencilEdgeHighlightsEnabled:
                stencilEdgeHighlightsEnabled);

        frame.SetIfStronger(
            x,
            y,
            new MatrixCell(
                character,
                intensity,
                priority));
    }

    private static MatrixIntensity ScaleIntensity(
        MatrixIntensity intensity,
        double weight,
        bool preserveLowIntensity)
    {
        if (intensity == MatrixIntensity.None ||
            weight < 0.125)
        {
            return MatrixIntensity.None;
        }

        if (preserveLowIntensity)
        {
            if (intensity ==
                MatrixIntensity.DeepShadow)
            {
                return MatrixIntensity.DeepShadow;
            }

            if (intensity ==
                MatrixIntensity.Far)
            {
                return weight >= 0.55
                    ? MatrixIntensity.Far
                    : MatrixIntensity.DeepShadow;
            }
        }

        var demotionLevels = weight switch
        {
            >= 0.875 => 0,
            >= 0.625 => 1,
            >= 0.375 => 2,
            _ => 3
        };

        var scaledValue =
            (int)intensity -
            demotionLevels;

        return scaledValue <=
               (int)MatrixIntensity.None
            ? MatrixIntensity.None
            : (MatrixIntensity)scaledValue;
    }

    private static MatrixIntensity GetIntensity(
        RainStream stream,
        int trailIndex)
    {
        return stream.LayerKind switch
        {
            MatrixLayerKind.Far =>
                GetFarIntensity(trailIndex),

            MatrixLayerKind.Middle =>
                GetMiddleIntensity(
                    stream,
                    trailIndex),

            MatrixLayerKind.Foreground =>
                GetForegroundIntensity(
                    stream,
                    trailIndex),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static MatrixIntensity GetFarIntensity(
        int trailIndex)
    {
        return trailIndex switch
        {
            0 => MatrixIntensity.Far,
            <= 2 => MatrixIntensity.Far,
            _ => MatrixIntensity.DeepShadow
        };
    }

    private static MatrixIntensity GetMiddleIntensity(
        RainStream stream,
        int trailIndex)
    {
        if (trailIndex == 0)
        {
            return MatrixIntensity.Normal;
        }

        if (IsSparseHighlight(
            stream,
            trailIndex,
            percentage: 2))
        {
            return MatrixIntensity.Bright;
        }

        return trailIndex switch
        {
            <= 3 => MatrixIntensity.Muted,
            <= 8 => MatrixIntensity.Far,
            _ => MatrixIntensity.DeepShadow
        };
    }

    private static MatrixIntensity GetForegroundIntensity(
        RainStream stream,
        int trailIndex)
    {
        if (trailIndex == 0)
        {
            return stream.IsHighlightMature
                ? MatrixIntensity.Highlight
                : MatrixIntensity.Normal;
        }

        if (stream.IsHighlightMature &&
            IsSparseHighlight(
                stream,
                trailIndex,
                percentage: 4))
        {
            return MatrixIntensity.Highlight;
        }

        return trailIndex switch
        {
            <= 2 => stream.IsHighlightMature
                ? MatrixIntensity.Bright
                : MatrixIntensity.Normal,

            <= 6 => MatrixIntensity.Normal,
            <= 11 => MatrixIntensity.Muted,
            _ => MatrixIntensity.Far
        };
    }

    private static bool IsSparseHighlight(
        RainStream stream,
        int trailIndex,
        int percentage)
    {
        var value =
            (uint)stream.Seed ^
            ((uint)trailIndex * 2_246_822_519u);

        return
            MatrixCharacterGenerator.Hash(value) %
            100 <
            percentage;
    }

    private static int GetLayerPriority(
        MatrixLayerKind kind)
    {
        return kind switch
        {
            MatrixLayerKind.Far => 0,
            MatrixLayerKind.Middle => 1,
            MatrixLayerKind.Foreground => 2,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind))
        };
    }
}