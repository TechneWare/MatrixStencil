using MatrixStencil.Core.Masks;
using MatrixStencil.Core.Rendering;
using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Animation;

public sealed class StencilOutlineAnimation
{
    private static readonly MatrixIntensity[] ChargeLevels =
    [
        MatrixIntensity.DeepShadow,
        MatrixIntensity.Far,
        MatrixIntensity.Muted,
        MatrixIntensity.Normal,
        MatrixIntensity.Bright,
        MatrixIntensity.Highlight
    ];

    private readonly StencilOutlineAnimationOptions _options;
    private readonly List<OutlineParticle> _particles = [];

    private double _phaseElapsedSeconds;
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
        StencilOutlineState.Dormant;

    public bool IsReleasing =>
        State == StencilOutlineState.Releasing;

    public bool SuppressStencilEdgeHighlights =>
        State != StencilOutlineState.Dormant;

    public int ParticleCount =>
        _particles.Count;

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

        if (phase == HeatPhase.PeakReveal)
        {
            if (!MatchesMask(mask) ||
                State == StencilOutlineState.Dormant)
            {
                CaptureOutline(mask);
            }

            if (State == StencilOutlineState.Charging)
            {
                _phaseElapsedSeconds +=
                    elapsed.TotalSeconds;

                if (IsFullyCharged())
                {
                    State = StencilOutlineState.Anchored;
                }
            }

            return;
        }

        if (State == StencilOutlineState.Charging ||
            State == StencilOutlineState.Anchored)
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

        if (State == StencilOutlineState.Dormant)
        {
            return;
        }

        foreach (var particle in _particles)
        {
            switch (State)
            {
                case StencilOutlineState.Charging:
                    RenderChargingParticle(
                        frame,
                        particle);
                    break;

                case StencilOutlineState.Anchored:
                    RenderAnchoredParticle(
                        frame,
                        particle);
                    break;

                case StencilOutlineState.Releasing:
                    RenderReleasedParticle(
                        frame,
                        particle);
                    break;
            }
        }
    }

    public void Reset()
    {
        _particles.Clear();
        _phaseElapsedSeconds = 0;
        _releaseElapsedSeconds = 0;
        _maskWidth = 0;
        _maskHeight = 0;
        _maskLeft = 0;
        _maskTop = 0;
        _maskRight = 0;
        _maskBottom = 0;
        State = StencilOutlineState.Dormant;
    }

    private void CaptureOutline(MessageMask mask)
    {
        _particles.Clear();
        _phaseElapsedSeconds = 0;
        _releaseElapsedSeconds = 0;
        _cycleNumber++;

        _maskWidth = mask.Width;
        _maskHeight = mask.Height;
        _maskLeft = mask.Left;
        _maskTop = mask.Top;
        _maskRight = mask.Right;
        _maskBottom = mask.Bottom;

        for (var y = mask.Top; y < mask.Bottom; y++)
        {
            for (var x = mask.Left; x < mask.Right; x++)
            {
                if (!mask.IsEdge(x, y))
                {
                    continue;
                }

                _particles.Add(
                    CreateParticle(
                        mask,
                        x,
                        y));
            }
        }

        State = _particles.Count > 0
            ? StencilOutlineState.Charging
            : StencilOutlineState.Dormant;
    }

    private OutlineParticle CreateParticle(
        MessageMask mask,
        int x,
        int y)
    {
        var baseHash =
            MatrixCharacterGenerator.Hash(
                unchecked(
                    (uint)(_cycleNumber * 1_103_515_245) ^
                    (uint)(x * 374_761_393) ^
                    (uint)(y * 668_265_263)));

        var activationHash =
            MatrixCharacterGenerator.Hash(
                baseHash ^ 0x9E37_79B9u);

        var releaseHash =
            MatrixCharacterGenerator.Hash(
                baseHash ^ 0x85EB_CA6Bu);

        var speedHash =
            MatrixCharacterGenerator.Hash(
                baseHash ^ 0xC2B2_AE35u);

        var rowSpan =
            Math.Max(
                1,
                mask.Bottom - mask.Top - 1);

        var normalizedY =
            (y - mask.Top) /
            (double)rowSpan;

        var rowBias =
            normalizedY * 0.35;

        var activationDelay =
            Math.Min(
                _options.MaximumActivationDelaySeconds,
                rowBias +
                Interpolate(
                    _options.MinimumActivationDelaySeconds,
                    _options.MaximumActivationDelaySeconds,
                    ToUnitInterval(activationHash)));

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

        var character =
            PickOutlineCharacter(baseHash);

        return new OutlineParticle(
            x,
            y,
            character,
            activationDelay,
            releaseDelay,
            speed,
            unchecked((int)baseHash));
    }

    private char PickOutlineCharacter(uint hash)
    {
        var index =
            (int)(hash %
                  (uint)_options.OutlineCharacters.Length);

        return _options.OutlineCharacters[index];
    }

    private void RenderChargingParticle(
        MatrixFrame frame,
        OutlineParticle particle)
    {
        if (_phaseElapsedSeconds <
            particle.ActivationDelaySeconds)
        {
            return;
        }

        var localElapsed =
            _phaseElapsedSeconds -
            particle.ActivationDelaySeconds;

        var chargeProgress =
            Math.Clamp(
                localElapsed / _options.ChargeDurationSeconds,
                0,
                1);

        var intensity =
            GetChargingIntensity(chargeProgress);

        RenderAtRow(
            frame,
            particle.Column,
            particle.StartRow,
            particle.OutlineCharacter,
            intensity);
    }

    private void RenderAnchoredParticle(
        MatrixFrame frame,
        OutlineParticle particle)
    {
        RenderAtRow(
            frame,
            particle.Column,
            particle.StartRow,
            particle.OutlineCharacter,
            MatrixIntensity.Highlight);
    }

    private void RenderReleasedParticle(
        MatrixFrame frame,
        OutlineParticle particle)
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

            return;
        }

        var fallDistance =
            particle.CurrentRow -
            particle.StartRow;

        var intensity =
            GetReleasedIntensity(fallDistance);

        var character =
            fallDistance < _options.MorphToMatrixAfterRows
                ? particle.OutlineCharacter
                : MatrixCharacterGenerator.Pick(
                    particle.Seed,
                    frameNumber:
                        (int)Math.Floor(
                            (fallDistance - _options.MorphToMatrixAfterRows) * 2.0),
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

        if (ghostIntensity != MatrixIntensity.None)
        {
            RenderAtRow(
                frame,
                particle.Column,
                particle.CurrentRow - 1.0,
                character,
                ghostIntensity);
        }
    }

    private void BeginRelease()
    {
        _releaseElapsedSeconds = 0;
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
            State = StencilOutlineState.Dormant;
            _releaseElapsedSeconds = 0;
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

    private bool IsFullyCharged()
    {
        var threshold =
            _particles.Count == 0
                ? 0
                : _particles.Max(
                    static particle =>
                        particle.ActivationDelaySeconds) +
                  _options.ChargeDurationSeconds;

        return _phaseElapsedSeconds >= threshold;
    }

    private static MatrixIntensity GetChargingIntensity(
        double progress)
    {
        var index =
            Math.Clamp(
                (int)Math.Floor(
                    progress * ChargeLevels.Length),
                0,
                ChargeLevels.Length - 1);

        return ChargeLevels[index];
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
            1);
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

    private bool MatchesMask(MessageMask mask)
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

    private static double ToUnitInterval(uint value)
    {
        return value /
               (double)uint.MaxValue;
    }

    private void ValidateOptions()
    {
        if (_options.MinimumActivationDelaySeconds < 0 ||
            _options.MaximumActivationDelaySeconds <
            _options.MinimumActivationDelaySeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    StencilOutlineAnimationOptions
                        .MaximumActivationDelaySeconds));
        }

        if (_options.ChargeDurationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    StencilOutlineAnimationOptions
                        .ChargeDurationSeconds));
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
            double activationDelaySeconds,
            double releaseDelaySeconds,
            double speedRowsPerSecond,
            int seed)
        {
            Column = column;
            StartRow = startRow;
            CurrentRow = startRow;
            OutlineCharacter = outlineCharacter;
            ActivationDelaySeconds = activationDelaySeconds;
            ReleaseDelaySeconds = releaseDelaySeconds;
            SpeedRowsPerSecond = speedRowsPerSecond;
            Seed = seed;
        }

        public int Column { get; }

        public double StartRow { get; }

        public double CurrentRow { get; set; }

        public char OutlineCharacter { get; }

        public double ActivationDelaySeconds { get; }

        public double ReleaseDelaySeconds { get; }

        public double SpeedRowsPerSecond { get; }

        public int Seed { get; }
    }
}