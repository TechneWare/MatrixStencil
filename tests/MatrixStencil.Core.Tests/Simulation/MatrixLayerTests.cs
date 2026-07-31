using MatrixStencil.Core.Randomness;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Tests.Simulation;

[TestFixture]
public sealed class MatrixLayerTests
{
    private static MatrixLayer CreateLayer()
    {
        return new MatrixLayer(
            new MatrixLayerOptions
            {
                Kind = MatrixLayerKind.Middle,
                TargetStreamsPerColumn = 0.5,
                SpawnRatePerColumnPerSecond = 1,
                MinimumSpeedRowsPerSecond = 10,
                MaximumSpeedRowsPerSecond = 10,
                MinimumTrailLength = 4,
                MaximumTrailLength = 4,
                MinimumHighlightDelayRows = 2,
                MaximumHighlightDelayRows = 2
            },
            new SeededRandomSource(123));
    }

    [Test]
    public void OpeningLayerSpawnsStreamsAtOrAboveTopOfScreen()
    {
        var layer = CreateLayer();
        layer.Open();
        layer.Update(TimeSpan.FromMilliseconds(100), width: 10, height: 20);

        Assert.That(layer.ActiveStreamCount, Is.GreaterThan(0));
        Assert.That(layer.Streams.All(stream => stream.BirthRow <= 0), Is.True);
    }

    [Test]
    public void ClosingLayerStopsSpawningButLetsExistingStreamsDrain()
    {
        var layer = CreateLayer();
        layer.Open();
        layer.Update(TimeSpan.FromSeconds(1), width: 10, height: 20);

        var spawnedBeforeClosing = layer.TotalSpawned;
        Assert.That(layer.ActiveStreamCount, Is.GreaterThan(0));

        layer.Close();
        layer.Update(TimeSpan.FromSeconds(10), width: 10, height: 20);

        Assert.Multiple(() =>
        {
            Assert.That(layer.TotalSpawned, Is.EqualTo(spawnedBeforeClosing));
            Assert.That(layer.ActiveStreamCount, Is.EqualTo(0));
            Assert.That(layer.State, Is.EqualTo(MatrixLayerState.Dormant));
        });
    }
}
