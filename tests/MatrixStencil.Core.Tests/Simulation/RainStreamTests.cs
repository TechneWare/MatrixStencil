using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Tests.Simulation;

[TestFixture]
public sealed class RainStreamTests
{
    [Test]
    public void HighlightMaturesOnlyAfterTravelingConfiguredRows()
    {
        var stream = new RainStream(
            MatrixLayerKind.Foreground,
            column: 2,
            headRow: 0,
            speedRowsPerSecond: 4,
            trailLength: 10,
            highlightDelayRows: 6,
            seed: 123);

        stream.Update(TimeSpan.FromSeconds(1));
        Assert.That(stream.IsHighlightMature, Is.False);

        stream.Update(TimeSpan.FromSeconds(0.5));
        Assert.That(stream.IsHighlightMature, Is.True);
    }

    [Test]
    public void HighlightDelayStartsAfterHeadEntersVisibleRows()
    {
        var stream = new RainStream(
            MatrixLayerKind.Foreground,
            column: 2,
            headRow: -5,
            speedRowsPerSecond: 5,
            trailLength: 10,
            highlightDelayRows: 3,
            seed: 123);

        stream.Update(TimeSpan.FromSeconds(1));
        Assert.That(stream.IsHighlightMature, Is.False);

        stream.Update(TimeSpan.FromSeconds(0.6));
        Assert.That(stream.IsHighlightMature, Is.True);
    }

    [Test]
    public void StreamDoesNotExitUntilTailHasClearedBottom()
    {
        var stream = new RainStream(
            MatrixLayerKind.Middle,
            column: 1,
            headRow: 10,
            speedRowsPerSecond: 5,
            trailLength: 8,
            highlightDelayRows: 0,
            seed: 5);

        Assert.That(stream.HasExited(screenHeight: 10), Is.False);

        stream.Update(TimeSpan.FromSeconds(1.6));
        Assert.That(stream.HasExited(screenHeight: 10), Is.True);
    }
}
