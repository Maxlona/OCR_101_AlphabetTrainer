using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OcrAlphabetTrainer.Core.Services;

/// <summary>
/// Splits an image of text into individual character images for OCR.
/// Uses a vertical projection profile: columns containing dark "ink" pixels belong to a
/// character, blank columns separate characters. The text line's baseline and cap height are
/// estimated from the glyph boxes, and each glyph is placed onto the model-input canvas via
/// <see cref="GlyphNormalizer"/> so its size and vertical position (baseline-relative) are
/// preserved — the same convention the training images are generated with.
/// </summary>
public class CharacterSegmenter
{
    /// <summary>
    /// A single segmented character: the path to its normalized image, and whether a wide
    /// blank gap (i.e. a word space) preceded it.
    /// </summary>
    public record CharSegment(string ImagePath, bool SpaceBefore);

    /// <summary>
    /// Segments the source image into per-character images saved into <paramref name="tempDir"/>.
    /// </summary>
    /// <param name="sourceImagePath">Path to the image to transcribe.</param>
    /// <param name="tempDir">Directory where the normalized character images are written.</param>
    /// <param name="inkThreshold">Luminance below this (0-255) counts as ink (dark pixel).</param>
    public static List<CharSegment> Segment(
        string sourceImagePath,
        string tempDir,
        byte inkThreshold = GlyphNormalizer.InkThreshold)
    {
        using var src = Image.Load<L8>(sourceImagePath);
        int width = src.Width;
        int height = src.Height;

        // Build an ink map once so segmentation is pure in-memory work afterwards.
        var ink = new bool[height, width];
        src.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    ink[y, x] = row[x].PackedValue < inkThreshold;
                }
            }
        });

        // Find horizontal runs of columns that contain ink -> candidate character x-ranges.
        var ranges = new List<(int Start, int End)>();
        int runStart = -1;
        for (int x = 0; x < width; x++)
        {
            bool columnHasInk = false;
            for (int y = 0; y < height; y++)
            {
                if (ink[y, x]) { columnHasInk = true; break; }
            }

            if (columnHasInk && runStart < 0)
            {
                runStart = x;
            }
            else if (!columnHasInk && runStart >= 0)
            {
                ranges.Add((runStart, x - 1));
                runStart = -1;
            }
        }
        if (runStart >= 0)
        {
            ranges.Add((runStart, width - 1));
        }

        Directory.CreateDirectory(tempDir);

        // Decide word spaces by comparing each blank gap to the *typical* inter-character gap:
        // within a word, gaps are small and similar; a word break is a clear outlier.
        var gaps = new List<int>();
        for (int i = 1; i < ranges.Count; i++)
        {
            gaps.Add(ranges[i].Start - ranges[i - 1].End - 1);
        }
        double medianGap = Median(gaps);
        double spaceThreshold = medianGap * 1.8;

        // Tight bounding box of the ink within a column range [s, e].
        Rectangle ColumnBox(int s, int e)
        {
            int top = height, bottom = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = s; x <= e; x++)
                {
                    if (ink[y, x])
                    {
                        if (y < top) top = y;
                        if (y > bottom) bottom = y;
                        break;
                    }
                }
            }
            return bottom < top ? Rectangle.Empty : new Rectangle(s, top, e - s + 1, bottom - top + 1);
        }

        // Bounding box of each detected run, and a robust estimate of a typical single-glyph width.
        var rawBoxes = ranges.Select(r => ColumnBox(r.Start, r.End)).ToList();
        var widths = rawBoxes.Where(b => b.Width > 0).Select(b => b.Width).ToList();
        double medianWidth = Median(widths);

        // Refine the runs into glyphs. A run much wider than a typical glyph usually contains
        // touching letters (common in bold/tight text), so split it into the implied number of
        // equal-width pieces. This recovers characters that column-projection would otherwise merge.
        var glyphs = new List<(Rectangle Box, bool SpaceBefore)>();
        int previousEnd = -1;
        for (int r = 0; r < ranges.Count; r++)
        {
            var (start, end) = ranges[r];
            var box = rawBoxes[r];
            if (box.Width <= 0)
            {
                continue;
            }

            bool spaceBefore = false;
            if (previousEnd >= 0)
            {
                int gap = start - previousEnd - 1;
                if (gaps.Count > 1 && gap > spaceThreshold)
                {
                    spaceBefore = true;
                }
            }
            previousEnd = end;

            int splitCount = 1;
            if (medianWidth > 0 && box.Width > medianWidth * 1.6)
            {
                splitCount = Math.Max(1, (int)Math.Round(box.Width / medianWidth));
            }

            if (splitCount <= 1)
            {
                glyphs.Add((box, spaceBefore));
            }
            else
            {
                int subWidth = (end - start + 1) / splitCount;
                for (int j = 0; j < splitCount; j++)
                {
                    int sx = start + j * subWidth;
                    int ex = (j == splitCount - 1) ? end : sx + subWidth - 1;
                    var subBox = ColumnBox(sx, ex);
                    if (subBox.Width > 0)
                    {
                        // Only the first piece can inherit a preceding word space.
                        glyphs.Add((subBox, j == 0 && spaceBefore));
                    }
                }
            }
        }

        // Estimate the text line's baseline and cap height from the refined glyph boxes.
        // Most glyphs rest on the baseline, so the median of their bottom edges is a robust baseline
        // (a few descenders like g/p/y don't skew the median). The cap height is the distance from
        // the baseline up to the highest ink (tallest cap or ascender).
        var validBoxes = glyphs.Where(g => g.Box.Height > 0).Select(g => g.Box).ToList();
        var bottoms = validBoxes.Select(b => b.Top + b.Height).ToList();
        double baselineY = Median(bottoms);
        int highestTop = validBoxes.Count > 0 ? validBoxes.Min(b => b.Top) : 0;
        double capHeight = Math.Max(1.0, baselineY - highestTop);

        var result = new List<CharSegment>();
        int index = 0;

        foreach (var (box, spaceBefore) in glyphs)
        {
            if (box.Height <= 0)
            {
                continue;
            }

            // Place the glyph using the shared baseline-relative convention (size + position preserved).
            using var canvas = GlyphNormalizer.RenderToCanvas(src, box, baselineY, capHeight);

            string outPath = Path.Combine(tempDir, $"seg_{index:D3}.png");
            canvas.SaveAsPng(outPath);
            result.Add(new CharSegment(outPath, spaceBefore));
            index++;
        }

        return result;
    }

    private static double Median(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }
}
