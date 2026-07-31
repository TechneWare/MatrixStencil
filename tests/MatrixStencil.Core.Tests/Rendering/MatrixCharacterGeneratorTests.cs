using MatrixStencil.Core.Rendering;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Tests.Rendering;

[TestFixture]
public sealed class MatrixCharacterGeneratorTests
{
    [Test]
    public void FarLayerCharacterRemainsStableAcrossFrames()
    {
        const int seed = 12345;
        const int trailIndex = 7;

        var initial =
            MatrixCharacterGenerator.Pick(
                seed,
                frameNumber: 0,
                trailIndex,
                MatrixLayerKind.Far);

        for (var frameNumber = 1;
             frameNumber <= 600;
             frameNumber++)
        {
            var current =
                MatrixCharacterGenerator.Pick(
                    seed,
                    frameNumber,
                    trailIndex,
                    MatrixLayerKind.Far);

            Assert.That(
                current,
                Is.EqualTo(initial));
        }
    }
}