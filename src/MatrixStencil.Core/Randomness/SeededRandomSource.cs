namespace MatrixStencil.Core.Randomness;

public sealed class SeededRandomSource : IRandomSource
{
    private readonly Random _random;

    public SeededRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int minimumInclusive, int maximumExclusive)
    {
        return _random.Next(minimumInclusive, maximumExclusive);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }
}
