namespace MatrixStencil.Core.Randomness;

public interface IRandomSource
{
    int Next(int minimumInclusive, int maximumExclusive);

    double NextDouble();
}
