using System;
using System.Text;
using OcrAlphabetTrainer.Core.Services;
using OcrAlphabetTrainer.Model.Prediction;

namespace OcrAlphabetTrainer.Console.Commands;

/// <summary>
/// Transcribes an image of text to a string: segments the image into individual
/// characters, classifies each with the trained model, and prints the assembled text.
/// </summary>
internal static class TranscribeCommand
{
    public static async Task ExecuteAsync(string imagePath, string modelPath)
    {
        System.Console.WriteLine($"📍 Image: {imagePath}");
        System.Console.WriteLine($"📍 Model: {modelPath}\n");

        try
        {
            if (!System.IO.File.Exists(imagePath))
            {
                System.Console.WriteLine($"❌ Image not found: {imagePath}");
                return;
            }

            if (!System.IO.File.Exists(modelPath))
            {
                System.Console.WriteLine($"❌ Model not found: {modelPath}");
                System.Console.WriteLine("   Run 'dotnet run -- train' first");
                return;
            }

            System.Console.WriteLine("🔍 Loading model...");
            var predictor = new OcrModelPredictor(modelPath);
            if (!await predictor.LoadModelAsync())
            {
                System.Console.WriteLine("❌ Failed to load model");
                return;
            }

            // Segment the image into individual characters in a throwaway temp folder.
            var tempDir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ocr_segments_" + Guid.NewGuid().ToString("N"));

            try
            {
                System.Console.WriteLine("\n✂️  Segmenting characters...");
                var segments = CharacterSegmenter.Segment(imagePath, tempDir);

                if (segments.Count == 0)
                {
                    System.Console.WriteLine("❌ No characters detected in the image");
                    return;
                }

                System.Console.WriteLine($"   Found {segments.Count} character(s)\n");

                var text = new StringBuilder();
                var details = new StringBuilder();

                foreach (var segment in segments)
                {
                    if (segment.SpaceBefore)
                    {
                        text.Append(' ');
                    }

                    var prediction = predictor.PredictImage(segment.ImagePath);
                    var letter = prediction?.PredictedLabel ?? "?";
                    text.Append(letter);

                    var confidence = prediction?.Confidence ?? 0f;
                    details.AppendLine($"     '{letter}' ({confidence:F1}%)");
                }

                System.Console.WriteLine("   Per-character predictions:");
                System.Console.Write(details.ToString());

                System.Console.WriteLine("\n📝 Transcribed text:");
                System.Console.WriteLine($"   {text}");
            }
            finally
            {
                try { System.IO.Directory.Delete(tempDir, recursive: true); } catch { /* best effort cleanup */ }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }
    }
}
