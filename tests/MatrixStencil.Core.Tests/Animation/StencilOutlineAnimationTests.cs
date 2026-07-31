using MatrixStencil.Core.Animation;
using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Rendering;

namespace MatrixStencil.Core.Tests.Animation;

[TestFixture]
public sealed class StencilOutlineAnimationTests
{
    [Test]
    public void PeakRevealStartsInChargingState()
    {
        var mask =
            new MessageMaskBuilder().Build(
                "Tony-Devs",
                120,
                40);

        var animation =
            new StencilOutlineAnimation();

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(
                animation.State,
                Is.EqualTo(
                    StencilOutlineState.Charging));

            Assert.That(
                animation.ParticleCount,
                Is.GreaterThan(0));

            Assert.That(
                animation.SuppressStencilEdgeHighlights,
                Is.True);
        });
    }

    [Test]
    public void ChargingEventuallyBecomesAnchored()
    {
        var mask =
            new MessageMaskBuilder().Build(
                "Tony-Devs",
                120,
                40);

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    MinimumActivationDelaySeconds = 0,
                    MaximumActivationDelaySeconds = 0,
                    ChargeDurationSeconds = 0.01
                });

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.Zero);

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.FromSeconds(0.1));

        Assert.That(
            animation.State,
            Is.EqualTo(
                StencilOutlineState.Anchored));
    }

    [Test]
    public void LeavingPeakRevealStartsRelease()
    {
        var mask =
            new MessageMaskBuilder().Build(
                "Tony-Devs",
                120,
                40);

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    MinimumActivationDelaySeconds = 0,
                    MaximumActivationDelaySeconds = 0,
                    ChargeDurationSeconds = 0.01
                });

        animation.Update(
            HeatPhase.PeakReveal,
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
            new MessageMaskBuilder().Build(
                "Tony-Devs",
                120,
                40);

        var animation =
            new StencilOutlineAnimation(
                new StencilOutlineAnimationOptions
                {
                    MinimumActivationDelaySeconds = 0,
                    MaximumActivationDelaySeconds = 0,
                    ChargeDurationSeconds = 0.01,
                    MinimumReleaseDelaySeconds = 0,
                    MaximumReleaseDelaySeconds = 0,
                    MinimumFallSpeedRowsPerSecond = 100,
                    MaximumFallSpeedRowsPerSecond = 100
                });

        animation.Update(
            HeatPhase.PeakReveal,
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
                    StencilOutlineState.Dormant));

            Assert.That(
                animation.ParticleCount,
                Is.Zero);
        });
    }

    [Test]
    public void ChargingRenderUsesForcedOutlineCharacters()
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
                    MinimumActivationDelaySeconds = 0,
                    MaximumActivationDelaySeconds = 0
                });

        animation.Update(
            HeatPhase.PeakReveal,
            mask,
            TimeSpan.FromSeconds(0.01));

        var frame =
            new MatrixFrame(80, 40);

        animation.Render(frame);

        var found =
            false;

        for (var y = mask.Top; y < mask.Bottom; y++)
        {
            for (var x = mask.Left; x < mask.Right; x++)
            {
                var cell = frame[x, y];

                if (cell.Character is '0' or '1' or '|' or ':')
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                break;
            }
        }

        Assert.That(found, Is.True);
    }
}