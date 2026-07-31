using MatrixStencil.Core.Glyphs;

namespace MatrixStencil.Core.Tests.Glyphs;

[TestFixture]
public sealed class GlyphCatalogTests
{
    [Test]
    public void SupportsEveryPrintableAsciiCharacter()
    {
        for (var code = GlyphCatalog.FirstPrintableAscii;
             code <= GlyphCatalog.LastPrintableAscii;
             code++)
        {
            Assert.That(
                GlyphCatalog.IsSupported((char)code),
                Is.True,
                $"Missing U+{code:X4}");
        }
    }

    [Test]
    public void EveryGlyphContainsEightRows()
    {
        foreach (var character in GlyphCatalog.SupportedCharacters)
        {
            var glyph = GlyphCatalog.Get(character);

            Assert.DoesNotThrow(() => glyph.GetRow(BitmapGlyph.Height - 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => glyph.GetRow(BitmapGlyph.Height));
        }
    }

    [TestCase('A')]
    [TestCase('a')]
    [TestCase('0')]
    [TestCase('@')]
    [TestCase('-')]
    public void VisibleGlyphsContainAtLeastOneSetPixel(char character)
    {
        var glyph = GlyphCatalog.Get(character);
        var hasPixel = false;

        for (var y = 0; y < BitmapGlyph.Height; y++)
        {
            for (var x = 0; x < BitmapGlyph.Width; x++)
            {
                hasPixel |= glyph.IsSet(x, y);
            }
        }

        Assert.That(hasPixel, Is.True);
    }
}
