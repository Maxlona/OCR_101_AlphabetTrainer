using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OcrAlphabetTrainer.Core.Services;

/// <summary>
/// Defines the single canvas convention shared by training-image generation and OCR segmentation,
/// so a glyph is placed onto the 64x64 model input the SAME way in both cases.
///
/// The key idea for punctuation: instead of centering each glyph (which erases the size and
/// vertical-position cues that tell '.' from "'" from "o"), every glyph is placed relative to a
/// common text baseline and scaled by one shared line factor. A period then lands low and small,
/// an apostrophe high and small, a capital fills the cap height — exactly as in real text.
/// </summary>
public static class GlyphNormalizer
{
    /// <summary>Side length of the square model input.</summary>
    public const int CanvasSize = 64;

    /// <summary>Y position of the text baseline on the canvas.</summary>
    public const double BaselineY = 46.0;

    /// <summary>Target cap height (baseline to top of capitals) on the canvas; sets the line scale.</summary>
    public const double CapHeight = 34.0;

    /// <summary>Default luminance threshold (0-255): pixels darker than this count as ink.</summary>
    public const byte InkThreshold = 128;

    /// <summary>
    /// Finds the tight bounding box of the ink (dark pixels) in a grayscale image.
    /// Returns false if the image has no ink.
    /// </summary>
    public static bool TryGetInkBounds(Image<L8> image, out Rectangle bounds, byte threshold = InkThreshold)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x].PackedValue < threshold)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
        });

        if (maxX < 0)
        {
            bounds = Rectangle.Empty;
            return false;
        }

        bounds = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return true;
    }

    /// <summary>
    /// Crops the glyph at <paramref name="glyphBox"/> from <paramref name="source"/> and renders it
    /// onto a fresh white 64x64 canvas, preserving its size and vertical position relative to the
    /// text line described by <paramref name="inputBaselineY"/> and <paramref name="inputCapHeight"/>.
    /// Horizontal position is centered (it carries no class information).
    /// </summary>
    public static Image<Rgba32> RenderToCanvas(
        Image<L8> source,
        Rectangle glyphBox,
        double inputBaselineY,
        double inputCapHeight)
    {
        // One scale factor for the whole line: map the line's cap height to the canvas cap height.
        double scale = CapHeight / Math.Max(1.0, inputCapHeight);

        int scaledWidth = Math.Max(1, (int)Math.Round(glyphBox.Width * scale));
        int scaledHeight = Math.Max(1, (int)Math.Round(glyphBox.Height * scale));

        using var glyph = source.Clone(ctx => ctx.Crop(glyphBox));
        using var scaled = glyph.Clone(ctx => ctx.Resize(scaledWidth, scaledHeight));

        var canvas = new Image<Rgba32>(CanvasSize, CanvasSize, Color.White);

        // How far the glyph's top sits above the baseline, in input pixels, then scaled to canvas.
        double topAboveBaseline = inputBaselineY - glyphBox.Top;
        int offsetY = (int)Math.Round(BaselineY - topAboveBaseline * scale);
        int offsetX = (CanvasSize - scaledWidth) / 2; // center horizontally

        canvas.Mutate(ctx => ctx.DrawImage(scaled, new Point(offsetX, offsetY), 1f));
        return canvas;
    }
}
