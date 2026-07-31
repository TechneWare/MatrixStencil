namespace MatrixStencil.Core.Rendering;

public sealed class MatrixFrame
{
    private readonly MatrixCell[] _cells;

    public MatrixFrame(int width, int height)
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
        _cells = Enumerable.Repeat(MatrixCell.Empty, width * height).ToArray();
    }

    public int Width { get; }

    public int Height { get; }

    public MatrixCell this[int x, int y] => _cells[(y * Width) + x];

    public void SetIfStronger(int x, int y, MatrixCell cell)
    {
        if (x is < 0 || x >= Width || y is < 0 || y >= Height)
        {
            return;
        }

        var index = (y * Width) + x;
        var current = _cells[index];

        if (cell.Intensity > current.Intensity ||
            (cell.Intensity == current.Intensity && cell.LayerPriority >= current.LayerPriority))
        {
            _cells[index] = cell;
        }
    }
}
