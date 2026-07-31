using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Rendering;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Tests.Rendering;

[TestFixture]
public sealed class FrameRendererImpactTests
{
    [Test]
    public void RendererReportsNaturalIntensityBeforeStencilPromotion()
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

        var stream =
            new RainStream(
                MatrixLayerKind.Far,
                x,
                y,
                speedRowsPerSecond: 1,
                trailLength: 1,
                highlightDelayRows: 0,
                seed: 1);

        var layer =
            new MatrixLayerSnapshot(
                MatrixLayerKind.Far,
                [stream]);

        var sink =
            new RecordingImpactSink();

        var renderer =
            new FrameRenderer();

        var frame =
            renderer.Render(
                width,
                height,
                [layer],
                mask,
                frameNumber: 0,
                peakRevealActive: false,
                stencilEdgeHighlightsEnabled: true,
                impactSink: sink);

        Assert.Multiple(() =>
        {
            Assert.That(
                sink.Impacts,
                Has.Count.EqualTo(1));

            Assert.That(
                sink.Impacts[0].Intensity,
                Is.EqualTo(
                    MatrixIntensity.Far));

            // The visible cell was promoted by the stencil mapper,
            // proving the captured value came from before mapping.
            Assert.That(
                frame[x, y].Intensity,
                Is.EqualTo(
                    MatrixIntensity.Highlight));
        });
    }

    private static (int X, int Y) FindEdge(
        MessageMask mask)
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
            "The mask contains no edge pixel.");
    }

    private sealed class RecordingImpactSink :
        IStencilImpactSink
    {
        public List<StencilImpact> Impacts { get; } = [];

        public void RegisterImpact(
            StencilImpact impact)
        {
            Impacts.Add(impact);
        }
    }
}