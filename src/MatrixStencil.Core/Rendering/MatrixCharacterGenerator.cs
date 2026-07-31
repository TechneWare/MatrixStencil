using MatrixStencil.Core.Simulation;

namespace MatrixStencil.Core.Rendering;

public static class MatrixCharacterGenerator
{
    private const string Characters =
        "0123456789" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcdefghijklmnopqrstuvwxyz" +
        "@#$%&*+<>[]{}?/\\|";

    /// <summary>
    /// Retained for callers that do not need layer-specific behavior.
    /// </summary>
    public static char Pick(
        int seed,
        int frameNumber,
        int trailIndex)
    {
        return Pick(
            seed,
            frameNumber,
            trailIndex,
            MatrixLayerKind.Foreground);
    }

    public static char Pick(
        int seed,
        int frameNumber,
        int trailIndex,
        MatrixLayerKind layerKind)
    {
        var mutationTick = GetMutationTick(
            seed,
            frameNumber,
            trailIndex,
            layerKind);

        var value =
            (uint)seed ^
            ((uint)mutationTick * 374_761_393u) ^
            ((uint)trailIndex * 668_265_263u);

        var hash = Hash(value);

        return Characters[
            (int)(hash % Characters.Length)];
    }

    public static uint Hash(uint value)
    {
        value ^= value >> 16;
        value *= 0x7FEB352Du;
        value ^= value >> 15;
        value *= 0x846CA68Bu;
        value ^= value >> 16;

        return value;
    }

    private static int GetMutationTick(
        int seed,
        int frameNumber,
        int trailIndex,
        MatrixLayerKind layerKind)
    {
        // Far-layer glyphs stay attached to the stream for its entire
        // lifetime. Their physical movement provides the animation.
        if (layerKind == MatrixLayerKind.Far)
        {
            return 0;
        }

        var mutationInterval = layerKind switch
        {
            MatrixLayerKind.Middle => 24,
            MatrixLayerKind.Foreground => 8,
            _ => throw new ArgumentOutOfRangeException(
                nameof(layerKind))
        };

        // Offset mutations by stream and trail position so an entire
        // stream does not change characters on the same frame.
        var phaseOffset =
            (int)((uint)seed %
                  (uint)mutationInterval);

        return (
            frameNumber +
            phaseOffset +
            (trailIndex * 3)) /
            mutationInterval;
    }
}