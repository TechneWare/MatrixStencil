using MatrixStencil.Core.Animation;
using MatrixStencil.Core.Randomness;

namespace MatrixStencil.Core.Simulation;

public sealed class MatrixSimulation
{
    private readonly MatrixSimulationOptions _options;
    private readonly HeatCycleController _heatCycle;
    private bool _middleGateOpen;
    private bool _foregroundGateOpen;

    public MatrixSimulation(
        int width,
        int height,
        int seed,
        MatrixSimulationOptions? options = null)
    {
        _options = options ?? new MatrixSimulationOptions();
        _heatCycle = new HeatCycleController(_options.HeatCycle);

        var masterRandom = new Random(seed);
        FarLayer = new MatrixLayer(
            _options.FarLayer,
            new SeededRandomSource(masterRandom.Next()));
        MiddleLayer = new MatrixLayer(
            _options.MiddleLayer,
            new SeededRandomSource(masterRandom.Next()));
        ForegroundLayer = new MatrixLayer(
            _options.ForegroundLayer,
            new SeededRandomSource(masterRandom.Next()));

        Resize(width, height);
    }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public MatrixLayer FarLayer { get; }

    public MatrixLayer MiddleLayer { get; }

    public MatrixLayer ForegroundLayer { get; }

    public HeatPhase Phase => _heatCycle.Phase;

    public bool IsPeakReveal =>
        Phase == HeatPhase.PeakReveal;

    public void Resize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        Restart();
    }

    public void Update(TimeSpan elapsed)
    {
        FarLayer.Update(elapsed, Width, Height);
        MiddleLayer.Update(elapsed, Width, Height);
        ForegroundLayer.Update(elapsed, Width, Height);

        var decision = _heatCycle.Update(
            elapsed,
            MiddleLayer.State,
            ForegroundLayer.State);

        ApplyGate(MiddleLayer, decision.MiddleGateOpen, ref _middleGateOpen);
        ApplyGate(ForegroundLayer, decision.ForegroundGateOpen, ref _foregroundGateOpen);
    }

    public void Restart()
    {
        _heatCycle.Restart();
        _middleGateOpen = false;
        _foregroundGateOpen = false;

        FarLayer.Reset(Width, Height, prepopulate: true);
        MiddleLayer.Reset(Width, Height, prepopulate: false);
        ForegroundLayer.Reset(Width, Height, prepopulate: false);
    }

    public IReadOnlyList<MatrixLayerSnapshot> CreateSnapshots()
    {
        return
        [
            FarLayer.CreateSnapshot(),
            MiddleLayer.CreateSnapshot(),
            ForegroundLayer.CreateSnapshot()
        ];
    }

    private static void ApplyGate(
        MatrixLayer layer,
        bool shouldBeOpen,
        ref bool currentGateState)
    {
        if (shouldBeOpen == currentGateState)
        {
            return;
        }

        currentGateState = shouldBeOpen;

        if (shouldBeOpen)
        {
            layer.Open();
        }
        else
        {
            layer.Close();
        }
    }
}
