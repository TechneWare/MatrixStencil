using MatrixStencil.Core.Rendering;

namespace MatrixStencil.ConsoleHost;

internal static class ConsolePalette
{
    public static ConsoleColorRgb Get(
        MatrixIntensity intensity)
    {
        return intensity switch
        {
            MatrixIntensity.None =>
                new(0, 0, 0),

            // Lifted slightly so screen recording and darker monitors do
            // not crush the entire far layer into black.
            MatrixIntensity.DeepShadow =>
                new(0, 46, 13),

            MatrixIntensity.Far =>
                new(0, 78, 22),

            MatrixIntensity.Muted =>
                new(0, 112, 34),

            MatrixIntensity.Normal =>
                new(0, 188, 64),

            MatrixIntensity.Bright =>
                new(40, 255, 105),

            MatrixIntensity.Highlight =>
                new(210, 255, 225),

            _ => throw new ArgumentOutOfRangeException(
                nameof(intensity))
        };
    }
}