using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OcrAlphabetTrainer.Core.Services;

/// <summary>
/// Generates synthetic training images for OCR model training.
/// Creates character images with various fonts and sizes.
/// </summary>
public class TrainingImageGenerator
{
    private readonly string _outputPath;
    private readonly Random _random = new();

    public TrainingImageGenerator(string outputPath)
    {
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
    }

    /// <summary>
    /// Generates training images for specified characters.
    /// </summary>
    public async Task GenerateTrainingImagesAsync(
        IEnumerable<char> characters,
        int samplesPerCharacter = 10,
        int imageWidth = 64,
        int imageHeight = 64,
        IEnumerable<string>? fontFamilies = null,
        bool useRotation = true,
        bool useNoise = false)
    {
        ArgumentNullException.ThrowIfNull(characters);

        // Use default Windows fonts if none provided
        fontFamilies ??= new[] { "Arial", "Times New Roman", "Courier New", "Calibri" };

        var fontFamilyList = fontFamilies.ToList();

        Console.WriteLine("📝 Generating training images...");

        foreach (var character in characters)
        {
            // Case-safe folder name so 'A' and 'a' don't collide on case-insensitive filesystems.
            var label = LabelCodec.Encode(character);
            var charDir = Path.Combine(_outputPath, label);
            Directory.CreateDirectory(charDir);

            for (int i = 0; i < samplesPerCharacter; i++)
            {
                try
                {
                    var fontFamily = fontFamilyList[_random.Next(fontFamilyList.Count)];
                    var fontSize = _random.Next(40, 56);

                    var image = GenerateNormalizedCharacterImage(character, fontFamily, fontSize);

                    var filename = Path.Combine(charDir, $"{label}_{i:D3}.png");
                    await image.SaveAsPngAsync(filename);
                    image.Dispose();

                    if ((i + 1) % 5 == 0)
                    {
                        Console.Write($".");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error generating image for {character} (sample {i}): {ex.Message}");
                }
            }

            Console.WriteLine($" ✓ {character} ({samplesPerCharacter} images)");
        }

        Console.WriteLine("✓ Image generation complete");
    }

    /// <summary>
    /// Renders a line of uppercase text as an image, one letter per cell with a clear blank
    /// gap between cells so the OCR segmenter can split it back into characters. Spaces in the
    /// text become empty cells (a wider gap the segmenter reads as a word break).
    /// Intended for creating test inputs for the 'transcribe' command.
    /// </summary>
    public void GenerateWordImage(
        string text,
        string outputPath,
        int height = 88,
        int fontSize = 44,
        string fontFamily = "Arial",
        int letterGap = 10,
        int spaceGap = 42,
        int margin = 16,
        int topY = 20)
    {
        var font = SystemFonts.CreateFont(fontFamily, fontSize);

        // Lay out glyphs left-to-right with a small gap between letters and a larger gap for spaces.
        // Every glyph uses the SAME vertical origin (topY), so they all share one font baseline —
        // this is what lets the segmenter recover the baseline and place punctuation correctly.
        float cursor = margin;
        var placements = new List<(string Ch, float X)>();
        foreach (var c in text)
        {
            if (c == ' ')
            {
                cursor += spaceGap;
                continue;
            }

            var ch = c.ToString();
            var size = TextMeasurer.MeasureSize(ch, new TextOptions(font));
            placements.Add((ch, cursor));
            cursor += size.Width + letterGap;
        }

        int width = (int)Math.Ceiling(cursor + margin);

        using var image = new Image<Rgba32>(Math.Max(width, 1), height, Color.White);

        image.Mutate(ctx =>
        {
            foreach (var (ch, x) in placements)
            {
                ctx.DrawText(new RichTextOptions(font) { Origin = new PointF(x, topY) }, ch, Color.Black);
            }
        });

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        image.SaveAsPng(outputPath);
    }

    /// <summary>
    /// Renders one character onto the normalized 64x64 model-input canvas, placed relative to the
    /// font's baseline so its size and vertical position are preserved (essential for telling
    /// punctuation like '.' / "'" apart). The baseline and cap height for this font+size are
    /// measured from a reference capital 'H' rendered the same way, so generation uses exactly the
    /// same placement convention as the OCR segmenter.
    /// </summary>
    private Image<Rgba32> GenerateNormalizedCharacterImage(char character, string fontFamily, int fontSize)
    {
        // Large temporary surface so any glyph (and the reference 'H') fits with margin.
        const int tempWidth = 200;
        const int tempHeight = 160;
        var origin = new PointF(60, 50);

        Font font;
        try
        {
            font = SystemFonts.CreateFont(fontFamily, fontSize);
        }
        catch
        {
            font = SystemFonts.CreateFont("Arial", fontSize);
        }

        // Reference 'H' establishes the baseline (its ink bottom) and cap height (its ink height).
        Rectangle refBox;
        using (var refImage = new Image<Rgba32>(tempWidth, tempHeight, Color.White))
        {
            refImage.Mutate(ctx => ctx.DrawText(new RichTextOptions(font) { Origin = origin }, "H", Color.Black));
            using var refGray = refImage.CloneAs<L8>();
            if (!GlyphNormalizer.TryGetInkBounds(refGray, out refBox))
            {
                return new Image<Rgba32>(GlyphNormalizer.CanvasSize, GlyphNormalizer.CanvasSize, Color.White);
            }
        }

        double baselineY = refBox.Top + refBox.Height;
        double capHeight = refBox.Height;

        // The target glyph, drawn at the same origin so it shares the reference's coordinate system.
        using var glyphImage = new Image<Rgba32>(tempWidth, tempHeight, Color.White);
        glyphImage.Mutate(ctx => ctx.DrawText(new RichTextOptions(font) { Origin = origin }, character.ToString(), Color.Black));
        using var glyphGray = glyphImage.CloneAs<L8>();

        if (!GlyphNormalizer.TryGetInkBounds(glyphGray, out var glyphBox))
        {
            return new Image<Rgba32>(GlyphNormalizer.CanvasSize, GlyphNormalizer.CanvasSize, Color.White);
        }

        return GlyphNormalizer.RenderToCanvas(glyphGray, glyphBox, baselineY, capHeight);
    }
}
