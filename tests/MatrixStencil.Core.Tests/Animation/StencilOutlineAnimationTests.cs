using MatrixStencil.Core.Animation;
using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Rendering;

namespace MatrixStencil.Core.Tests.Animation;

[TestFixture]
public sealed class StencilOutlineAnimationTests
{
    [Test]
    public void OpeningMiddleStartsCollectingImpacts()
    {
        var mask =
            CreateMask();

        var animation =
            new StencilOutlineAnimation();

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(
                animation.State,
                Is.EqualTo(
                    StencilOutlineState.Collecting));

            Assert.That(
                animation.ParticleCount,
                Is.GreaterThan(0));

            Assert.That(
                animation.SuppressStencilEdgeHighlights,
                Is.True);
        });
    }

    [Test]
    public void CapturedImpactSticksToOutline()
    {
        var mask =
            CreateMask();

        var animation =
            new StencilOutlineAnimation();

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        var (x, y) =
            FindEdge(mask);

        animation.RegisterImpact(
            new StencilImpact(
                x,
                y,
                MatrixIntensity.Normal));

        var frame =
            new MatrixFrame(
                mask.Width,
                mask.Height);

        animation.Render(frame);

        Assert.Multiple(() =>
        {
            Assert.That(
                frame[x, y].Intensity,
                Is.EqualTo(
                    MatrixIntensity.Normal));

            Assert.That(
                frame[x, y].Character,
                Is.AnyOf('0', '1', '|', ':'));
        });
    }

    [Test]
    public void WeakerImpactDoesNotDimCapturedPixel()
    {
        var mask =
            CreateMask();

        var animation =
            new StencilOutlineAnimation();

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        var (x, y) =
            FindEdge(mask);

        animation.RegisterImpact(
            new StencilImpact(
                x,
                y,
                MatrixIntensity.Bright));

        animation.RegisterImpact(
            new StencilImpact(
                x,
                y,
                MatrixIntensity.Far));

        var frame =
            new MatrixFrame(
                mask.Width,
                mask.Height);

        animation.Render(frame);

        Assert.That(
            frame[x, y].Intensity,
            Is.EqualTo(MatrixIntensity.Bright));
    }

    [Test]
    public void EqualizationRaisesOnlyPixelsBelowCurrentFloor()
    {
        var mask =
            CreateMask();

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    EqualizationDurationSeconds = 1.0
                });

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        var edges =
            FindTwoEdges(mask);

        animation.RegisterImpact(
            new StencilImpact(
                edges.First.X,
                edges.First.Y,
                MatrixIntensity.Normal));

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.FromSeconds(0.5));

        var frame =
            new MatrixFrame(
                mask.Width,
                mask.Height);

        animation.Render(frame);

        Assert.Multiple(() =>
        {
            // Already-normal pixels do not reset or become dimmer.
            Assert.That(
                frame[
                    edges.First.X,
                    edges.First.Y].Intensity,
                Is.EqualTo(
                    MatrixIntensity.Normal));

            // A never-hit pixel catches up to the current Muted floor.
            Assert.That(
                frame[
                    edges.Second.X,
                    edges.Second.Y].Intensity,
                Is.EqualTo(
                    MatrixIntensity.Muted));
        });
    }

    [Test]
    public void EqualizationEventuallyHighlightsCompleteOutline()
    {
        var mask =
            CreateMask();

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    EqualizationDurationSeconds = 0.1
                });

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.FromSeconds(0.2));

        var frame =
            new MatrixFrame(
                mask.Width,
                mask.Height);

        animation.Render(frame);

        var (x, y) =
            FindEdge(mask);

        Assert.Multiple(() =>
        {
            Assert.That(
                animation.State,
                Is.EqualTo(
                    StencilOutlineState.Anchored));

            Assert.That(
                frame[x, y].Intensity,
                Is.EqualTo(
                    MatrixIntensity.Highlight));
        });
    }

    [Test]
    public void LeavingPeakRevealStartsRelease()
    {
        var mask =
            CreateMask();

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    EqualizationDurationSeconds = 0.01
                });

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.FromSeconds(0.1));

        animation.Update(
            HeatPhase.ClosingForeground,
            mask,
            TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(
                animation.State,
                Is.EqualTo(
                    StencilOutlineState.Releasing));

            Assert.That(
                animation.IsReleasing,
                Is.True);
        });
    }

    [Test]
    public void ReleasedOutlineEventuallyFallsOffScreen()
    {
        var mask =
            CreateMask();

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    EqualizationDurationSeconds = 0.01,
                    MinimumReleaseDelaySeconds = 0,
                    MaximumReleaseDelaySeconds = 0,
                    MinimumFallSpeedRowsPerSecond = 100,
                    MaximumFallSpeedRowsPerSecond = 100
                });

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.FromSeconds(0.1));

        animation.Update(
            HeatPhase.ClosingForeground,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.ClosingForeground,
            mask,
            TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(
                animation.State,
                Is.EqualTo(
                    StencilOutlineState.Cooling));

            Assert.That(
                animation.ParticleCount,
                Is.Zero);
        });
    }

    [Test]
    public void CompletedReleaseRemainsSuppressedUntilNextCollectionPhase()
    {
        var mask =
            new MessageMaskBuilder().Build(
                "A",
                80,
                40);

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    EqualizationDurationSeconds = 0.01,
                    MinimumReleaseDelaySeconds = 0,
                    MaximumReleaseDelaySeconds = 0,
                    MinimumFallSpeedRowsPerSecond = 100,
                    MaximumFallSpeedRowsPerSecond = 100
                });

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.FromSeconds(0.1));

        animation.Update(
            HeatPhase.ClosingForeground,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.ClosingForeground,
            mask,
            TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(
                animation.State,
                Is.EqualTo(
                    StencilOutlineState.Cooling));

            Assert.That(
                animation.SuppressStencilEdgeHighlights,
                Is.True);

            Assert.That(
                animation.ParticleCount,
                Is.Zero);
        });

        animation.Update(
            HeatPhase.ClosingMiddle,
            mask,
            TimeSpan.FromSeconds(1));

        animation.Update(
            HeatPhase.ColdHold,
            mask,
            TimeSpan.FromSeconds(1));

        Assert.That(
            animation.State,
            Is.EqualTo(
                StencilOutlineState.Cooling));

        animation.Update(
            HeatPhase.OpeningMiddle,
            mask,
            TimeSpan.Zero);

        Assert.That(
            animation.State,
            Is.EqualTo(
                StencilOutlineState.Collecting));
    }

    private static MessageMask CreateMask()
    {
        return new MessageMaskBuilder().Build(
            "A",
            80,
            40);
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

    private static (
        (int X, int Y) First,
        (int X, int Y) Second)
        FindTwoEdges(MessageMask mask)
    {
        var edges =
            new List<(int X, int Y)>();

        for (var y = mask.Top;
             y < mask.Bottom;
             y++)
        {
            for (var x = mask.Left;
                 x < mask.Right;
                 x++)
            {
                if (!mask.IsEdge(x, y))
                {
                    continue;
                }

                edges.Add((x, y));

                if (edges.Count == 2)
                {
                    return (
                        edges[0],
                        edges[1]);
                }
            }
        }

        throw new AssertionException(
            "The mask contains fewer than two edge pixels.");
    }
}