using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Rendering;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Tests.Rendering;

[TestFixture]
public sealed class FrameRendererTests
{
    [Test]
    public void CharacterInsideStencilUsesLayerZeroIntensity()
    {
        const int width = 80;
        const int height = 40;

        var mask =
            new MessageMaskBuilder().Build(
                "A",
                width,
                height);

        var (x, y) =
            FindInteriorPixel(mask);

        var stream = CreateStream(
            MatrixLayerKind.Foreground,
            x,
            y,
            highlightDelayRows: 0);

        var frame = Render(
            width,
            height,
            mask,
            stream);

        Assert.Multiple(() =>
        {
            Assert.That(
                frame[x, y].Intensity,
                Is.EqualTo(MatrixIntensity.DeepShadow));

            Assert.That(
                frame[x, y].Character,
                Is.Not.EqualTo(' '));
        });
    }

    [Test]
    public void CharacterOnAnyStencilEdgeUsesHighlightIntensity()
    {
        const int width = 80;
        const int height = 40;

        var mask =
            new MessageMaskBuilder().Build(
                "A",
                width,
                height);

        var (x, y) =
            FindEdge(mask);

        var stream = CreateStream(
            MatrixLayerKind.Far,
            x,
            y);

        var frame = Render(
            width,
            height,
            mask,
            stream);

        Assert.That(
            frame[x, y].Intensity,
            Is.EqualTo(MatrixIntensity.Highlight));
    }

    [Test]
    public void CharacterAdjacentToStencilEdgeUsesBrightIntensity()
    {
        const int width = 80;
        const int height = 40;

        var mask =
            new MessageMaskBuilder().Build(
                "A",
                width,
                height);

        var (x, y) =
            FindAdjacentToEdge(mask);

        var stream = CreateStream(
            MatrixLayerKind.Far,
            x,
            y);

        var frame = Render(
            width,
            height,
            mask,
            stream);

        Assert.That(
            frame[x, y].Intensity,
            Is.EqualTo(MatrixIntensity.Bright));
    }

    [Test]
    public void FractionalFarStreamRemainsVisibleAcrossAdjacentRows()
    {
        const int width = 80;
        const int height = 40;
        const int x = 0;

        var mask =
            new MessageMaskBuilder().Build(
                "A",
                width,
                height);

        var stream = CreateStream(
            MatrixLayerKind.Far,
            x,
            headRow: 10.5);

        var frame = Render(
            width,
            height,
            mask,
            stream);

        Assert.Multiple(() =>
        {
            Assert.That(
                frame[x, 10].Intensity,
                Is.Not.EqualTo(MatrixIntensity.None));

            Assert.That(
                frame[x, 11].Intensity,
                Is.Not.EqualTo(MatrixIntensity.None));
        });
    }
    [Test]
    public void PeakRevealDemotesForegroundStencilCharacterOneLevel()
    {
        const int width = 80;
        const int height = 40;

        var mask =
            new MessageMaskBuilder().Build(
                "A",
                width,
                height);

        var (x, y) =
            FindInteriorPixel(mask);

        var stream = CreateStream(
            MatrixLayerKind.Foreground,
            x,
            y,
            highlightDelayRows: 0);

        var frame = Render(
            width,
            height,
            mask,
            stream,
            peakRevealActive: true);

        Assert.That(
            frame[x, y].Intensity,
            Is.EqualTo(MatrixIntensity.Bright));
    }
    private static MatrixFrame Render(
        int width,
        int height,
        MessageMask mask,
        RainStream stream,
        bool peakRevealActive = false)
    {
        var renderer =
            new FrameRenderer();

        var layer =
            new MatrixLayerSnapshot(
                stream.LayerKind,
                [stream]);

        return renderer.Render(
            width,
            height,
            [layer],
            mask,
            frameNumber: 0,
            peakRevealActive:
                peakRevealActive);
    }

    private static RainStream CreateStream(
        MatrixLayerKind layerKind,
        int column,
        double headRow,
        int highlightDelayRows = 0)
    {
        return new RainStream(
            layerKind,
            column,
            headRow,
            speedRowsPerSecond: 1,
            trailLength: 1,
            highlightDelayRows:
                highlightDelayRows,
            seed: 1);
    }

    private static (int X, int Y)
        FindInteriorPixel(MessageMask mask)
    {
        for (var y = mask.Top;
             y < mask.Bottom;
             y++)
        {
            for (var x = mask.Left;
                 x < mask.Right;
                 x++)
            {
                if (mask.Contains(x, y) &&
                    !mask.IsEdge(x, y))
                {
                    return (x, y);
                }
            }
        }

        throw new AssertionException(
            "Mask contains no interior pixel.");
    }

    private static (int X, int Y)
        FindEdge(MessageMask mask)
    {
        for (var y = mask.Top;
             y < mask.Bottom;
             y++)
        {
            for (var x = mask.Left;
                 x < mask.Right;
                 x++)
            {
                if (mask.IsEdge(x, y))
                {
                    return (x, y);
                }
            }
        }

        throw new AssertionException(
            "Mask contains no edge pixel.");
    }

    private static (int X, int Y)
        FindAdjacentToEdge(MessageMask mask)
    {
        for (var y = 0;
             y < mask.Height;
             y++)
        {
            for (var x = 0;
                 x < mask.Width;
                 x++)
            {
                if (mask.IsAdjacentToEdge(x, y))
                {
                    return (x, y);
                }
            }
        }

        throw new AssertionException(
            "Mask contains no exterior edge-adjacent pixel.");
    }

}