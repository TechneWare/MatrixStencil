namespace MatrixStencil.Core.Glyphs;

public sealed class BitmapGlyph
{
    public const int Width = 8;
    public const int Height = 8;

    private readonly byte[] _rows;

    public BitmapGlyph(params byte[] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Length != Height)
        {
            throw new ArgumentException(
                $"A glyph must contain exactly {Height} rows.",
                nameof(rows));
        }

        _rows = rows.ToArray();
    }

    public byte GetRow(int y)
    {
        if (y is < 0 or >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return _rows[y];
    }

    public bool IsSet(int x, int y)
    {
        if (x is < 0 or >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        var mask = 1 << (Width - 1 - x);
        return (GetRow(y) & mask) != 0;
    }
}