using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Animation;

public sealed class HeatCycleController
{
    private readonly HeatCycleOptions _options;
    private TimeSpan _phaseElapsed;

    public HeatCycleController(
        HeatCycleOptions? options = null)
    {
        _options =
            options ??
            HeatCycleOptions.Default;
    }

    public HeatPhase Phase { get; private set; } =
        HeatPhase.ColdHold;

    public HeatCycleDecision CurrentDecision =>
        CreateDecision();

    public HeatCycleDecision Update(
        TimeSpan elapsed,
        MatrixLayerState middleState,
        MatrixLayerState foregroundState)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsed));
        }

        _phaseElapsed += elapsed;

        switch (Phase)
        {
            case HeatPhase.ColdHold:
                TransitionAfter(
                    _options.ColdHoldDuration,
                    HeatPhase.OpeningMiddle);
                break;

            case HeatPhase.OpeningMiddle:
                TransitionAfter(
                    _options.MiddleWarmupDuration,
                    HeatPhase.OpeningForeground);
                break;

            case HeatPhase.OpeningForeground:
                TransitionAfter(
                    _options.ForegroundWarmupDuration,
                    HeatPhase.HotHold);
                break;

            case HeatPhase.HotHold:
                TransitionAfter(
                    _options.HotHoldDuration,
                    HeatPhase.PeakReveal);
                break;

            case HeatPhase.PeakReveal:
                TransitionAfter(
                    _options.PeakRevealDuration,
                    HeatPhase.ClosingForeground);
                break;

            case HeatPhase.ClosingForeground:
                if (foregroundState ==
                    MatrixLayerState.Dormant)
                {
                    TransitionTo(
                        HeatPhase.ClosingMiddle);
                }

                break;

            case HeatPhase.ClosingMiddle:
                if (middleState ==
                    MatrixLayerState.Dormant)
                {
                    TransitionTo(
                        HeatPhase.ColdHold);
                }

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return CreateDecision();
    }

    public void Restart()
    {
        Phase = HeatPhase.ColdHold;
        _phaseElapsed = TimeSpan.Zero;
    }

    private void TransitionAfter(
        TimeSpan duration,
        HeatPhase nextPhase)
    {
        if (_phaseElapsed >= duration)
        {
            TransitionTo(nextPhase);
        }
    }

    private void TransitionTo(
        HeatPhase nextPhase)
    {
        Phase = nextPhase;
        _phaseElapsed = TimeSpan.Zero;
    }

    private HeatCycleDecision CreateDecision()
    {
        return Phase switch
        {
            HeatPhase.ColdHold =>
                new(Phase, false, false),

            HeatPhase.OpeningMiddle =>
                new(Phase, true, false),

            HeatPhase.OpeningForeground =>
                new(Phase, true, true),

            HeatPhase.HotHold =>
                new(Phase, true, true),

            HeatPhase.PeakReveal =>
                new(Phase, true, true),

            HeatPhase.ClosingForeground =>
                new(Phase, true, false),

            HeatPhase.ClosingMiddle =>
                new(Phase, false, false),

            _ => throw new ArgumentOutOfRangeException()
        };
    }
}