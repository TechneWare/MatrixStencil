using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Rendering;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Animation;

public sealed class StencilOutlineAnimation : IStencilImpactSink
{
    private static readonly MatrixIntensity[] EqualizationLevels =
    [
        MatrixIntensity.None,
        MatrixIntensity.DeepShadow,
        MatrixIntensity.Far,
        MatrixIntensity.Muted,
        MatrixIntensity.Normal,
        MatrixIntensity.Bright,
        MatrixIntensity.Highlight
    ];

    private readonly StencilOutlineAnimationOptions _options;
    private readonly List<OutlineParticle> _particles = [];
    private readonly Dictionary<int, OutlineParticle> _particlesByPosition = [];

    private double _equalizationElapsedSeconds;
    private double _releaseElapsedSeconds;
    private int _cycleNumber;

    private int _maskWidth;
    private int _maskHeight;
    private int _maskLeft;
    private int _maskTop;
    private int _maskRight;
    private int _maskBottom;

    public StencilOutlineAnimation(
        StencilOutlineAnimationOptions? options = null)
    {
        _options =
            options ??
            new StencilOutlineAnimationOptions();

        ValidateOptions();
    }

    public StencilOutlineState State { get; private set; } =
        StencilOutlineState.Cooling;

    public bool IsReleasing =>
        State == StencilOutlineState.Releasing;

    /// <summary>
    /// Once outline collection begins, this animation owns the stencil
    /// perimeter. The normal renderer should stop adding temporary edge
    /// promotions behind it.
    /// </summary>
    public bool SuppressStencilEdgeHighlights =>
        State is not StencilOutlineState.Dormant;

    public int ParticleCount =>
        _particles.Count;

    public void RegisterImpact(StencilImpact impact)
    {
        if (State is not StencilOutlineState.Collecting and
            not StencilOutlineState.Equalizing)
        {
            return;
        }

        if (impact.Intensity == MatrixIntensity.None)
        {
            return;
        }

        if (impact.X < 0 ||
            impact.X >= _maskWidth ||
            impact.Y < 0 ||
            impact.Y >= _maskHeight)
        {
            return;
        }

        var key = GetPositionKey(
            impact.X,
            impact.Y);

        if (!_particlesByPosition.TryGetValue(
            key,
            out var particle))
        {
            return;
        }

        particle.HasCapturedImpact = true;

        // A stronger impact promotes the stored pixel. A weaker impact
        // never dims or resets it.
        if (impact.Intensity >
            particle.CapturedIntensity)
        {
            particle.CapturedIntensity =
                impact.Intensity;
        }
    }

    public void Update(
        HeatPhase phase,
        MessageMask mask,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(mask);

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsed));
        }

        if (IsCollectionPhase(phase))
        {
            if (State is
                    StencilOutlineState.Dormant or
                    StencilOutlineState.Cooling ||
                !MatchesMask(mask))
            {
                InitializeOutline(mask);
            }

            return;
        }

        if (phase == HeatPhase.PeakReveal)
        {
            if (State is
                    StencilOutlineState.Dormant or
                    StencilOutlineState.Cooling ||
                !MatchesMask(mask))
            {
                InitializeOutline(mask);
            }

            if (State == StencilOutlineState.Collecting)
            {
                BeginEqualization();
            }

            if (State == StencilOutlineState.Equalizing)
            {
                AdvanceEqualization(elapsed);
            }

            return;
        }

        if (State is StencilOutlineState.Collecting or
            StencilOutlineState.Equalizing or
            StencilOutlineState.Anchored)
        {
            BeginRelease();
        }

        if (State == StencilOutlineState.Releasing)
        {
            AdvanceRelease(elapsed);
        }
    }

    public void Render(MatrixFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        switch (State)
        {
            case StencilOutlineState.Dormant:
            case StencilOutlineState.Cooling:
                return;

            case StencilOutlineState.Collecting:
                RenderCollectedImpacts(frame);
                return;

            case StencilOutlineState.Equalizing:
                RenderEqualizingOutline(frame);
                return;

            case StencilOutlineState.Anchored:
                RenderAnchoredOutline(frame);
                return;

            case StencilOutlineState.Releasing:
                RenderReleasedOutline(frame);
                return;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Reset()
    {
        _particles.Clear();
        _particlesByPosition.Clear();

        _equalizationElapsedSeconds = 0;
        _releaseElapsedSeconds = 0;

        _maskWidth = 0;
        _maskHeight = 0;
        _maskLeft = 0;
        _maskTop = 0;
        _maskRight = 0;
        _maskBottom = 0;

        State = StencilOutlineState.Cooling;
    }

    private static bool IsCollectionPhase(
        HeatPhase phase)
    {
        return phase is
            HeatPhase.OpeningMiddle or
            HeatPhase.OpeningForeground or
            HeatPhase.HotHold;
    }

    private void InitializeOutline(
        MessageMask mask)
    {
        _particles.Clear();
        _particlesByPosition.Clear();

        _equalizationElapsedSeconds = 0;
        _releaseElapsedSeconds = 0;
        _cycleNumber++;

        _maskWidth = mask.Width;
        _maskHeight = mask.Height;
        _maskLeft = mask.Left;
        _maskTop = mask.Top;
        _maskRight = mask.Right;
        _maskBottom = mask.Bottom;

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

                var particle =
                    CreateParticle(x, y);

                _particles.Add(particle);

                _particlesByPosition.Add(
                    GetPositionKey(x, y),
                    particle);
            }
        }

        State = _particles.Count > 0
            ? StencilOutlineState.Collecting
            : StencilOutlineState.Cooling;
    }

    private OutlineParticle CreateParticle(
        int x,
        int y)
    {
        var baseHash =
            MatrixCharacterGenerator.Hash(
                unchecked(
                    (uint)(_cycleNumber * 1_103_515_245) ^
                    (uint)(x * 374_761_393) ^
                    (uint)(y * 668_265_263)));

        var releaseHash =
            MatrixCharacterGenerator.Hash(
                baseHash ^
                0x9E37_79B9u);

        var speedHash =
            MatrixCharacterGenerator.Hash(
                baseHash ^
                0x85EB_CA6Bu);

        var releaseDelay =
            Interpolate(
                _options.MinimumReleaseDelaySeconds,
                _options.MaximumReleaseDelaySeconds,
                ToUnitInterval(releaseHash));

        var speed =
            Interpolate(
                _options.MinimumFallSpeedRowsPerSecond,
                _options.MaximumFallSpeedRowsPerSecond,
                ToUnitInterval(speedHash));

        var outlineCharacter =
            PickOutlineCharacter(baseHash);

        return new OutlineParticle(
            x,
            y,
            outlineCharacter,
            releaseDelay,
            speed,
            unchecked((int)baseHash));
    }

    private char PickOutlineCharacter(uint hash)
    {
        var index =
            (int)(
                hash %
                (uint)_options.OutlineCharacters.Length);

        return _options.OutlineCharacters[index];
    }

    private void BeginEqualization()
    {
        _equalizationElapsedSeconds = 0;
        State = StencilOutlineState.Equalizing;
    }

    private void AdvanceEqualization(
        TimeSpan elapsed)
    {
        _equalizationElapsedSeconds +=
            elapsed.TotalSeconds;

        if (_equalizationElapsedSeconds >=
            _options.EqualizationDurationSeconds)
        {
            State = StencilOutlineState.Anchored;
        }
    }

    private void RenderCollectedImpacts(
        MatrixFrame frame)
    {
        foreach (var particle in _particles)
        {
            if (!particle.HasCapturedImpact)
            {
                continue;
            }

            RenderAtRow(
                frame,
                particle.Column,
                particle.StartRow,
                particle.OutlineCharacter,
                particle.CapturedIntensity);
        }
    }

    private void RenderEqualizingOutline(
        MatrixFrame frame)
    {
        var revealFloor =
            GetEqualizationFloor();

        foreach (var particle in _particles)
        {
            var capturedIntensity =
                particle.HasCapturedImpact
                    ? particle.CapturedIntensity
                    : MatrixIntensity.None;

            var effectiveIntensity =
                Max(
                    capturedIntensity,
                    revealFloor);

            if (effectiveIntensity ==
                MatrixIntensity.None)
            {
                continue;
            }

            RenderAtRow(
                frame,
                particle.Column,
                particle.StartRow,
                particle.OutlineCharacter,
                effectiveIntensity);
        }
    }

    private void RenderAnchoredOutline(
        MatrixFrame frame)
    {
        foreach (var particle in _particles)
        {
            RenderAtRow(
                frame,
                particle.Column,
                particle.StartRow,
                particle.OutlineCharacter,
                MatrixIntensity.Highlight);
        }
    }

    private void RenderReleasedOutline(
        MatrixFrame frame)
    {
        foreach (var particle in _particles)
        {
            if (_releaseElapsedSeconds <
                particle.ReleaseDelaySeconds)
            {
                RenderAtRow(
                    frame,
                    particle.Column,
                    particle.StartRow,
                    particle.OutlineCharacter,
                    MatrixIntensity.Highlight);

                continue;
            }

            var fallDistance =
                particle.CurrentRow -
                particle.StartRow;

            var intensity =
                GetReleasedIntensity(
                    fallDistance);

            var character =
                fallDistance <
                _options.MorphToMatrixAfterRows
                    ? particle.OutlineCharacter
                    : MatrixCharacterGenerator.Pick(
                        particle.Seed,
                        frameNumber:
                            (int)Math.Floor(
                                (fallDistance -
                                 _options.MorphToMatrixAfterRows) *
                                2.0),
                        trailIndex: 0,
                        MatrixLayerKind.Foreground);

            RenderAtRow(
                frame,
                particle.Column,
                particle.CurrentRow,
                character,
                intensity);

            var ghostIntensity =
                DemoteOneLevel(intensity);

            if (ghostIntensity !=
                MatrixIntensity.None)
            {
                RenderAtRow(
                    frame,
                    particle.Column,
                    particle.CurrentRow - 1.0,
                    character,
                    ghostIntensity);
            }
        }
    }

    private MatrixIntensity GetEqualizationFloor()
    {
        var progress =
            Math.Clamp(
                _equalizationElapsedSeconds /
                _options.EqualizationDurationSeconds,
                0,
                1);

        var maximumIndex =
            EqualizationLevels.Length - 1;

        var levelIndex =
            Math.Min(
                maximumIndex,
                (int)Math.Floor(
                    progress * maximumIndex));

        return EqualizationLevels[levelIndex];
    }

    private void BeginRelease()
    {
        _releaseElapsedSeconds = 0;

        foreach (var particle in _particles)
        {
            particle.CurrentRow =
                particle.StartRow;
        }

        State = StencilOutlineState.Releasing;
    }

    private void AdvanceRelease(
        TimeSpan elapsed)
    {
        var previousElapsed =
            _releaseElapsedSeconds;

        _releaseElapsedSeconds +=
            elapsed.TotalSeconds;

        foreach (var particle in _particles)
        {
            var previousActiveTime =
                Math.Max(
                    0,
                    previousElapsed -
                    particle.ReleaseDelaySeconds);

            var currentActiveTime =
                Math.Max(
                    0,
                    _releaseElapsedSeconds -
                    particle.ReleaseDelaySeconds);

            var activeElapsed =
                currentActiveTime -
                previousActiveTime;

            if (activeElapsed <= 0)
            {
                continue;
            }

            particle.CurrentRow +=
                particle.SpeedRowsPerSecond *
                activeElapsed;
        }

        _particles.RemoveAll(
            particle =>
                particle.CurrentRow >
                _maskHeight + 2);

        if (_particles.Count == 0)
        {
            _particlesByPosition.Clear();
            _releaseElapsedSeconds = 0;

            // Keep the stencil disabled for the rest of cooling and the
            // following cold hold. OpeningMiddle starts the next collection.
            State = StencilOutlineState.Cooling;
        }
    }

    private void RenderAtRow(
        MatrixFrame frame,
        int x,
        double row,
        char character,
        MatrixIntensity intensity)
    {
        var baseRow =
            (int)Math.Floor(row);

        var fractionalRow =
            row - baseRow;

        RenderSample(
            frame,
            x,
            baseRow,
            character,
            intensity,
            1.0 - fractionalRow);

        RenderSample(
            frame,
            x,
            baseRow + 1,
            character,
            intensity,
            fractionalRow);
    }

    private void RenderSample(
        MatrixFrame frame,
        int x,
        int y,
        char character,
        MatrixIntensity intensity,
        double weight)
    {
        if (x < 0 ||
            x >= frame.Width ||
            y < 0 ||
            y >= frame.Height)
        {
            return;
        }

        var weightedIntensity =
            ApplyFractionalWeight(
                intensity,
                weight);

        if (weightedIntensity ==
            MatrixIntensity.None)
        {
            return;
        }

        frame.SetIfStronger(
            x,
            y,
            new MatrixCell(
                character,
                weightedIntensity,
                _options.RenderPriority));
    }

    private MatrixIntensity GetReleasedIntensity(
        double fallDistance)
    {
        if (fallDistance <
            _options.HighlightDistanceRows)
        {
            return MatrixIntensity.Highlight;
        }

        if (fallDistance <
            _options.BrightDistanceRows)
        {
            return MatrixIntensity.Bright;
        }

        if (fallDistance <
            _options.NormalDistanceRows)
        {
            return MatrixIntensity.Normal;
        }

        if (fallDistance <
            _options.MutedDistanceRows)
        {
            return MatrixIntensity.Muted;
        }

        return MatrixIntensity.Far;
    }

    private static MatrixIntensity ApplyFractionalWeight(
        MatrixIntensity intensity,
        double weight)
    {
        if (weight < 0.125)
        {
            return MatrixIntensity.None;
        }

        var demotionLevels = weight switch
        {
            >= 0.75 => 0,
            >= 0.40 => 1,
            _ => 2
        };

        return Demote(
            intensity,
            demotionLevels);
    }

    private static MatrixIntensity DemoteOneLevel(
        MatrixIntensity intensity)
    {
        return Demote(
            intensity,
            levels: 1);
    }

    private static MatrixIntensity Demote(
        MatrixIntensity intensity,
        int levels)
    {
        var value =
            (int)intensity -
            levels;

        return value <=
               (int)MatrixIntensity.None
            ? MatrixIntensity.None
            : (MatrixIntensity)value;
    }

    private static MatrixIntensity Max(
        MatrixIntensity first,
        MatrixIntensity second)
    {
        return first >= second
            ? first
            : second;
    }

    private int GetPositionKey(
        int x,
        int y)
    {
        return (y * _maskWidth) + x;
    }

    private bool MatchesMask(
        MessageMask mask)
    {
        return
            mask.Width == _maskWidth &&
            mask.Height == _maskHeight &&
            mask.Left == _maskLeft &&
            mask.Top == _maskTop &&
            mask.Right == _maskRight &&
            mask.Bottom == _maskBottom;
    }

    private static double Interpolate(
        double minimum,
        double maximum,
        double amount)
    {
        return minimum +
               ((maximum - minimum) * amount);
    }

    private static double ToUnitInterval(
        uint value)
    {
        return value /
               (double)uint.MaxValue;
    }

    private void ValidateOptions()
    {
        if (_options.EqualizationDurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    StencilOutlineAnimationOptions
                        .EqualizationDurationSeconds));
        }

        if (_options.MinimumReleaseDelaySeconds < 0 ||
            _options.MaximumReleaseDelaySeconds <
            _options.MinimumReleaseDelaySeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    StencilOutlineAnimationOptions
                        .MaximumReleaseDelaySeconds));
        }

        if (_options.MinimumFallSpeedRowsPerSecond <= 0 ||
            _options.MaximumFallSpeedRowsPerSecond <
            _options.MinimumFallSpeedRowsPerSecond)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    StencilOutlineAnimationOptions
                        .MaximumFallSpeedRowsPerSecond));
        }

        if (string.IsNullOrWhiteSpace(
            _options.OutlineCharacters))
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    StencilOutlineAnimationOptions
                        .OutlineCharacters));
        }
    }

    private sealed class OutlineParticle
    {
        public OutlineParticle(
            int column,
            int startRow,
            char outlineCharacter,
            double releaseDelaySeconds,
            double speedRowsPerSecond,
            int seed)
        {
            Column = column;
            StartRow = startRow;
            CurrentRow = startRow;
            OutlineCharacter = outlineCharacter;
            ReleaseDelaySeconds =
                releaseDelaySeconds;
            SpeedRowsPerSecond =
                speedRowsPerSecond;
            Seed = seed;
        }

        public int Column { get; }

        public double StartRow { get; }

        public double CurrentRow { get; set; }

        public char OutlineCharacter { get; }

        public double ReleaseDelaySeconds { get; }

        public double SpeedRowsPerSecond { get; }

        public int Seed { get; }

        public bool HasCapturedImpact { get; set; }

        public MatrixIntensity CapturedIntensity { get; set; } =
            MatrixIntensity.None;
    }
}