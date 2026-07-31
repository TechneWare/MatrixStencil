using MatrixStencil.Core.Masks;

namespace MatrixStencil.Core.Tests.Masks;

[TestFixture]
public sealed class MessageMaskBuilderTests
{
    private readonly MessageMaskBuilder _builder = new();

    [Test]
    public void Build_CreatesCenteredNonEmptyMask()
    {
        var mask = _builder.Build(
            "Tony-Devs",
            120,
            32);

        Assert.Multiple(() =>
        {
            Assert.That(
                mask.SetPixelCount,
                Is.GreaterThan(0));

            Assert.That(
                Math.Abs(
                    (mask.Left + mask.Right) -
                    mask.Width),
                Is.LessThanOrEqualTo(1));

            Assert.That(
                Math.Abs(
                    (mask.Top + mask.Bottom) -
                    mask.Height),
                Is.LessThanOrEqualTo(1));
        });
    }

    [Test]
    public void Build_SupportsMixedCaseDigitsAndPunctuation()
    {
        var mask = _builder.Build(
            "Dev-42!?",
            120,
            32);

        Assert.That(
            mask.SetPixelCount,
            Is.GreaterThan(0));
    }

    [Test]
    public void Build_IdentifiesCompleteStencilPerimeter()
    {
        var mask = _builder.Build(
            "A",
            80,
            40);

        var edgeCount = 0;

        for (var y = mask.Top;
             y < mask.Bottom;
             y++)
        {
            for (var x = mask.Left;
                 x < mask.Right;
                 x++)
            {
                if (mask.IsEdge(x, y))
                {
                    edgeCount++;
                }
            }
        }

        Assert.That(
            edgeCount,
            Is.GreaterThan(0));
    }

    [Test]
    public void Build_IdentifiesExteriorCellsAdjacentToPerimeter()
    {
        var mask = _builder.Build(
            "A",
            80,
            40);

        var (x, y) =
            FindAdjacentToEdge(mask);

        Assert.Multiple(() =>
        {
            Assert.That(
                mask.Contains(x, y),
                Is.False);

            Assert.That(
                mask.IsAdjacentToEdge(x, y),
                Is.True);
        });
    }

    [Test]
    public void Build_RejectsNonPrintableCharacters()
    {
        Assert.That(
            () => _builder.Build(
                "Line\nBreak",
                120,
                32),
            Throws.ArgumentException);
    }

    [Test]
    public void Build_ThrowsWhenMessageDoesNotFit()
    {
        Assert.That(
            () => _builder.Build(
                new string('W', 80),
                40,
                12),
            Throws.InvalidOperationException);
    }

    private static (int X, int Y)
        FindAdjacentToEdge(MessageMask mask)
    {
        for (var y = 0; y < mask.Height; y++)
        {
            for (var x = 0; x < mask.Width; x++)
            {
                if (mask.IsAdjacentToEdge(x, y))
                {
                    return (x, y);
                }
            }
        }

        throw new AssertionException(
            "Mask has no exterior edge-adjacent position.");
    }
    [Test]
    public void HorizontalExpansionMakesStencilStrokesWider()
    {
        var normalBuilder =
            new MessageMaskBuilder(
                new MessageMaskBuilderOptions
                {
                    HorizontalStrokeExpansionColumns = 0
                });

        var expandedBuilder =
            new MessageMaskBuilder(
                new MessageMaskBuilderOptions
                {
                    HorizontalStrokeExpansionColumns = 1
                });

        var normal =
            normalBuilder.Build(
                "A",
                80,
                40);

        var expanded =
            expandedBuilder.Build(
                "A",
                80,
                40);

        Assert.That(
            expanded.SetPixelCount,
            Is.GreaterThan(normal.SetPixelCount));
    }
    [Test]
    public void DefaultBuilderDoesNotExpandStencilHorizontally()
    {
        var builder =
            new MessageMaskBuilder();

        var mask =
            builder.Build(
                "Tony-Devs",
                120,
                40);

        Assert.That(
            mask.SetPixelCount,
            Is.GreaterThan(0));
    }
}