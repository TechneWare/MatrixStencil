using MatrixStencil.Core.Rendering;

namespace MatrixStencil.Core.Tests.Rendering;

[TestFixture]
public sealed class StencilMapperTests
{
    private readonly StencilMapper _mapper = new();

    [Test]
    public void OutsideStencilLeavesIntensityUnchanged()
    {
        var result = _mapper.Map(
            MatrixIntensity.Normal,
            insideStencil: false,
            onStencilEdge: false,
            adjacentToStencilEdge: false,
            peakRevealActive: false);

        Assert.That(
            result,
            Is.EqualTo(MatrixIntensity.Normal));
    }

    [TestCase(MatrixIntensity.DeepShadow)]
    [TestCase(MatrixIntensity.Far)]
    [TestCase(MatrixIntensity.Muted)]
    [TestCase(MatrixIntensity.Normal)]
    [TestCase(MatrixIntensity.Bright)]
    [TestCase(MatrixIntensity.Highlight)]
    public void NormalStencilInteriorUsesLayerZeroIntensity(
        MatrixIntensity input)
    {
        var result = _mapper.Map(
            input,
            insideStencil: true,
            onStencilEdge: false,
            adjacentToStencilEdge: false,
            peakRevealActive: false);

        Assert.That(
            result,
            Is.EqualTo(MatrixIntensity.DeepShadow));
    }

    [TestCase(
        MatrixIntensity.DeepShadow,
        MatrixIntensity.DeepShadow)]
    [TestCase(
        MatrixIntensity.Far,
        MatrixIntensity.DeepShadow)]
    [TestCase(
        MatrixIntensity.Muted,
        MatrixIntensity.Far)]
    [TestCase(
        MatrixIntensity.Normal,
        MatrixIntensity.Muted)]
    [TestCase(
        MatrixIntensity.Bright,
        MatrixIntensity.Normal)]
    [TestCase(
        MatrixIntensity.Highlight,
        MatrixIntensity.Bright)]
    public void PeakRevealDemotesStencilIntensityOneLevel(
        MatrixIntensity input,
        MatrixIntensity expected)
    {
        var result = _mapper.Map(
            input,
            insideStencil: true,
            onStencilEdge: false,
            adjacentToStencilEdge: false,
            peakRevealActive: true);

        Assert.That(
            result,
            Is.EqualTo(expected));
    }

    [Test]
    public void StencilEdgeAlwaysUsesHighlightIntensity()
    {
        var result = _mapper.Map(
            MatrixIntensity.DeepShadow,
            insideStencil: true,
            onStencilEdge: true,
            adjacentToStencilEdge: false,
            peakRevealActive: true);

        Assert.That(
            result,
            Is.EqualTo(MatrixIntensity.Highlight));
    }

    [Test]
    public void AdjacentPositionUsesAtLeastBrightIntensity()
    {
        var result = _mapper.Map(
            MatrixIntensity.DeepShadow,
            insideStencil: false,
            onStencilEdge: false,
            adjacentToStencilEdge: true,
            peakRevealActive: false);

        Assert.That(
            result,
            Is.EqualTo(MatrixIntensity.Bright));
    }

    [Test]
    public void EmptyPositionRemainsEmpty()
    {
        var result = _mapper.Map(
            MatrixIntensity.None,
            insideStencil: true,
            onStencilEdge: true,
            adjacentToStencilEdge: false,
            peakRevealActive: true);

        Assert.That(
            result,
            Is.EqualTo(MatrixIntensity.None));
    }
    [Test]
    public void EdgeHighlightCanBeDisabledWhileOutlineReleases()
    {
        var result = _mapper.Map(
            MatrixIntensity.Normal,
            insideStencil: true,
            onStencilEdge: true,
            adjacentToStencilEdge: false,
            peakRevealActive: false,
            stencilEdgeHighlightsEnabled: false);

        Assert.That(
            result,
            Is.EqualTo(
                MatrixIntensity.DeepShadow));
    }
    [Test]
    public void EdgeHighlightsCanBeSuppressedWhileOutlineOwnsTheReveal()
    {
        var result = _mapper.Map(
            MatrixIntensity.Normal,
            insideStencil: true,
            onStencilEdge: true,
            adjacentToStencilEdge: false,
            peakRevealActive: false,
            stencilEdgeHighlightsEnabled: false);

        Assert.That(
            result,
            Is.EqualTo(MatrixIntensity.DeepShadow));
    }
}