using MatrixStencil.Core.Animation;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Tests.Animation;

[TestFixture]
public sealed class HeatCycleControllerTests
{
    private static HeatCycleController CreateController()
    {
        return new HeatCycleController(
            new HeatCycleOptions
            {
                ColdHoldDuration =
                    TimeSpan.FromSeconds(1),

                MiddleWarmupDuration =
                    TimeSpan.FromSeconds(1),

                ForegroundWarmupDuration =
                    TimeSpan.FromSeconds(1),

                HotHoldDuration =
                    TimeSpan.FromSeconds(1),

                PeakRevealDuration =
                    TimeSpan.FromSeconds(1)
            });
    }

    [Test]
    public void Cycle_OpensLayersPeaksAndClosesInReverse()
    {
        var controller =
            CreateController();

        var middle =
            MatrixLayerState.Dormant;

        var foreground =
            MatrixLayerState.Dormant;

        var decision = controller.Update(
            TimeSpan.FromSeconds(1.1),
            middle,
            foreground);

        Assert.Multiple(() =>
        {
            Assert.That(
                decision.Phase,
                Is.EqualTo(
                    HeatPhase.OpeningMiddle));

            Assert.That(
                decision.MiddleGateOpen,
                Is.True);

            Assert.That(
                decision.ForegroundGateOpen,
                Is.False);
        });

        middle =
            MatrixLayerState.Opening;

        decision = controller.Update(
            TimeSpan.FromSeconds(1.1),
            middle,
            foreground);

        Assert.Multiple(() =>
        {
            Assert.That(
                decision.Phase,
                Is.EqualTo(
                    HeatPhase.OpeningForeground));

            Assert.That(
                decision.ForegroundGateOpen,
                Is.True);
        });

        foreground =
            MatrixLayerState.Opening;

        decision = controller.Update(
            TimeSpan.FromSeconds(1.1),
            middle,
            foreground);

        Assert.That(
            decision.Phase,
            Is.EqualTo(HeatPhase.HotHold));

        decision = controller.Update(
            TimeSpan.FromSeconds(1.1),
            middle,
            foreground);

        Assert.Multiple(() =>
        {
            Assert.That(
                decision.Phase,
                Is.EqualTo(HeatPhase.PeakReveal));

            Assert.That(
                decision.MiddleGateOpen,
                Is.True);

            Assert.That(
                decision.ForegroundGateOpen,
                Is.True);
        });

        decision = controller.Update(
            TimeSpan.FromSeconds(1.1),
            middle,
            foreground);

        Assert.Multiple(() =>
        {
            Assert.That(
                decision.Phase,
                Is.EqualTo(
                    HeatPhase.ClosingForeground));

            Assert.That(
                decision.MiddleGateOpen,
                Is.True);

            Assert.That(
                decision.ForegroundGateOpen,
                Is.False);
        });

        decision = controller.Update(
            TimeSpan.FromSeconds(10),
            middle,
            MatrixLayerState.Closing);

        Assert.That(
            decision.Phase,
            Is.EqualTo(
                HeatPhase.ClosingForeground));

        decision = controller.Update(
            TimeSpan.Zero,
            middle,
            MatrixLayerState.Dormant);

        Assert.Multiple(() =>
        {
            Assert.That(
                decision.Phase,
                Is.EqualTo(
                    HeatPhase.ClosingMiddle));

            Assert.That(
                decision.MiddleGateOpen,
                Is.False);
        });

        decision = controller.Update(
            TimeSpan.Zero,
            MatrixLayerState.Dormant,
            MatrixLayerState.Dormant);

        Assert.That(
            decision.Phase,
            Is.EqualTo(HeatPhase.ColdHold));
    }
}