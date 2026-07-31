namespace MatrixStencil.Core.Animation;

public readonly record struct HeatCycleDecision(
    HeatPhase Phase,
    bool MiddleGateOpen,
    bool ForegroundGateOpen);
