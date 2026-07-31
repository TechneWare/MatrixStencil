namespace MatrixStencil.Core.Masks;

public sealed class MessageMask
{
    private readonly bool[] _pixels;
    private readonly bool[] _edgePixels;
    private readonly bool[] _adjacentToEdgePixels;

    internal MessageMask(
        int width,
        int height,
        bool[] pixels,
        bool[] edgePixels,
        bool[] adjacentToEdgePixels,
        int left,
        int top,
        int right,
        int bottom)
    {
        Width = width;
        Height = height;
        _pixels = pixels;
        _edgePixels = edgePixels;
        _adjacentToEdgePixels = adjacentToEdgePixels;
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public int Width { get; }

    public int Height { get; }

    public int Left { get; }

    public int Top { get; }

    public int Right { get; }

    public int Bottom { get; }

    public int SetPixelCount =>
        _pixels.Count(static value => value);

    public bool Contains(int x, int y)
    {
        return GetValue(_pixels, x, y);
    }

    /// <summary>
    /// Returns true when the position is part of the stencil and touches
    /// at least one position outside the stencil.
    /// </summary>
    public bool IsEdge(int x, int y)
    {
        return GetValue(_edgePixels, x, y);
    }

    /// <summary>
    /// Returns true when the position is outside the stencil but directly
    /// borders one of its edge positions.
    /// </summary>
    public bool IsAdjacentToEdge(int x, int y)
    {
        return GetValue(_adjacentToEdgePixels, x, y);
    }

    private bool GetValue(bool[] values, int x, int y)
    {
        if (x is < 0 || x >= Width ||
            y is < 0 || y >= Height)
        {
            return false;
        }

        return values[(y * Width) + x];
    }
}