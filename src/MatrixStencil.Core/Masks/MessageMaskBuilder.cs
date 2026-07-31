using MatrixStencil.Core.Glyphs;

namespace MatrixStencil.Core.Masks;

public sealed class MessageMaskBuilder
{
    private readonly MessageMaskBuilderOptions _options;

    public MessageMaskBuilder(
        MessageMaskBuilderOptions? options = null)
    {
        _options =
            options ??
            new MessageMaskBuilderOptions();
    }

    public MessageMask Build(
        string message,
        int screenWidth,
        int screenHeight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            message);

        if (screenWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(screenWidth));
        }

        if (screenHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(screenHeight));
        }

        if (_options.HorizontalStrokeExpansionColumns < 0)
        {
            throw new InvalidOperationException(
                $"{nameof(MessageMaskBuilderOptions.HorizontalStrokeExpansionColumns)} " +
                "cannot be negative.");
        }

        ValidateCharacters(message);

        var expansion =
            _options.HorizontalStrokeExpansionColumns;

        var unscaledWidth =
            (message.Length * BitmapGlyph.Width) +
            ((message.Length - 1) *
             _options.GlyphSpacingPixels);

        var availableWidth =
            screenWidth -
            (_options.MarginColumns * 2) -
            (expansion * 2);

        var availableHeight =
            screenHeight -
            (_options.MarginRows * 2);

        var widthScale =
            availableWidth / unscaledWidth;

        var heightScale =
            availableHeight / BitmapGlyph.Height;

        var scale = Math.Min(
            _options.MaximumScale,
            Math.Min(widthScale, heightScale));

        if (scale < 1)
        {
            throw new InvalidOperationException(
                $"The message '{message}' does not fit inside a " +
                $"{screenWidth}x{screenHeight} console.");
        }

        var unexpandedRenderedWidth =
            unscaledWidth * scale;

        var renderedWidth =
            unexpandedRenderedWidth +
            (expansion * 2);

        var renderedHeight =
            BitmapGlyph.Height * scale;

        var left =
            (screenWidth - renderedWidth) / 2;

        var top =
            (screenHeight - renderedHeight) / 2;

        var glyphStartX =
            left + expansion;

        var pixels =
            new bool[screenWidth * screenHeight];

        var cursorX = glyphStartX;

        foreach (var character in message)
        {
            var glyph =
                GlyphCatalog.Get(character);

            for (var glyphY = 0;
                 glyphY < BitmapGlyph.Height;
                 glyphY++)
            {
                for (var glyphX = 0;
                     glyphX < BitmapGlyph.Width;
                     glyphX++)
                {
                    if (!glyph.IsSet(
                        glyphX,
                        glyphY))
                    {
                        continue;
                    }

                    FillScaledPixel(
                        pixels,
                        screenWidth,
                        cursorX + (glyphX * scale),
                        top + (glyphY * scale),
                        scale);
                }
            }

            cursorX +=
                (BitmapGlyph.Width +
                 _options.GlyphSpacingPixels) *
                scale;
        }

        pixels = ExpandHorizontally(
            pixels,
            screenWidth,
            screenHeight,
            expansion);

        var (
            edgePixels,
            adjacentToEdgePixels) =
            BuildEdgeMasks(
                pixels,
                screenWidth,
                screenHeight);

        return new MessageMask(
            screenWidth,
            screenHeight,
            pixels,
            edgePixels,
            adjacentToEdgePixels,
            left,
            top,
            left + renderedWidth,
            top + renderedHeight);
    }

    private static void ValidateCharacters(
        string message)
    {
        foreach (var character in message)
        {
            if (!GlyphCatalog.IsSupported(character))
            {
                throw new ArgumentException(
                    $"Message contains unsupported character " +
                    $"U+{(int)character:X4}.",
                    nameof(message));
            }
        }
    }

    private static void FillScaledPixel(
        bool[] pixels,
        int screenWidth,
        int x,
        int y,
        int scale)
    {
        for (var offsetY = 0;
             offsetY < scale;
             offsetY++)
        {
            for (var offsetX = 0;
                 offsetX < scale;
                 offsetX++)
            {
                var index =
                    ((y + offsetY) * screenWidth) +
                    x +
                    offsetX;

                pixels[index] = true;
            }
        }
    }

    private static bool[] ExpandHorizontally(
        bool[] source,
        int width,
        int height,
        int expansion)
    {
        if (expansion == 0)
        {
            return source;
        }

        var expanded =
            source.ToArray();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index =
                    (y * width) + x;

                if (!source[index])
                {
                    continue;
                }

                for (var offsetX = -expansion;
                     offsetX <= expansion;
                     offsetX++)
                {
                    var expandedX =
                        x + offsetX;

                    if (expandedX < 0 ||
                        expandedX >= width)
                    {
                        continue;
                    }

                    expanded[
                        (y * width) +
                        expandedX] = true;
                }
            }
        }

        return expanded;
    }

    private static (
        bool[] EdgePixels,
        bool[] AdjacentToEdgePixels)
        BuildEdgeMasks(
            bool[] pixels,
            int width,
            int height)
    {
        var edgePixels =
            new bool[pixels.Length];

        var adjacentToEdgePixels =
            new bool[pixels.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index =
                    (y * width) + x;

                if (!pixels[index])
                {
                    continue;
                }

                if (HasOutsideNeighbor(
                    pixels,
                    width,
                    height,
                    x,
                    y))
                {
                    edgePixels[index] = true;
                }
            }
        }

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index =
                    (y * width) + x;

                if (!edgePixels[index])
                {
                    continue;
                }

                for (var offsetY = -1;
                     offsetY <= 1;
                     offsetY++)
                {
                    for (var offsetX = -1;
                         offsetX <= 1;
                         offsetX++)
                    {
                        if (offsetX == 0 &&
                            offsetY == 0)
                        {
                            continue;
                        }

                        var neighborX =
                            x + offsetX;

                        var neighborY =
                            y + offsetY;

                        if (neighborX < 0 ||
                            neighborX >= width ||
                            neighborY < 0 ||
                            neighborY >= height)
                        {
                            continue;
                        }

                        var neighborIndex =
                            (neighborY * width) +
                            neighborX;

                        if (!pixels[neighborIndex])
                        {
                            adjacentToEdgePixels[
                                neighborIndex] = true;
                        }
                    }
                }
            }
        }

        return (
            edgePixels,
            adjacentToEdgePixels);
    }

    private static bool HasOutsideNeighbor(
        bool[] pixels,
        int width,
        int height,
        int x,
        int y)
    {
        for (var offsetY = -1;
             offsetY <= 1;
             offsetY++)
        {
            for (var offsetX = -1;
                 offsetX <= 1;
                 offsetX++)
            {
                if (offsetX == 0 &&
                    offsetY == 0)
                {
                    continue;
                }

                var neighborX =
                    x + offsetX;

                var neighborY =
                    y + offsetY;

                if (neighborX < 0 ||
                    neighborX >= width ||
                    neighborY < 0 ||
                    neighborY >= height)
                {
                    return true;
                }

                var neighborIndex =
                    (neighborY * width) +
                    neighborX;

                if (!pixels[neighborIndex])
                {
                    return true;
                }
            }
        }

        return false;
    }
}