using System;
using OcrAlphabetTrainer.Model.Prediction;

namespace OcrAlphabetTrainer.Console.Commands;

/// <summary>
/// Predicts the character in a single image.
/// </summary>
internal static class PredictCommand
{
    public static async Task ExecuteAsync(string imagePath, string modelPath)
    {
        System.Console.WriteLine($"📍 Image: {imagePath}");
        System.Console.WriteLine($"📍 Model: {modelPath}\n");

        try
        {
            // Check if image exists
            if (!System.IO.File.Exists(imagePath))
            {
                System.Console.WriteLine($"❌ Image not found: {imagePath}");
                return;
            }

            // Check if model exists
            if (!System.IO.File.Exists(modelPath))
            {
                System.Console.WriteLine($"❌ Model not found: {modelPath}");
                System.Console.WriteLine("   Run 'dotnet run -- train' first");
                return;
            }

            System.Console.WriteLine("🔍 Loading model...");
            var predictor = new OcrModelPredictor(modelPath);
            var loaded = await predictor.LoadModelAsync();

            if (!loaded)
            {
                System.Console.WriteLine("❌ Failed to load model");
                return;
            }

            System.Console.WriteLine("\n🔮 Making prediction...");
            var prediction = predictor.PredictImage(imagePath);

            if (prediction != null)
            {
                System.Console.WriteLine("\n📊 Prediction Result:");
                System.Console.WriteLine($"   Image: {System.IO.Path.GetFileName(imagePath)}");
                System.Console.WriteLine($"   Predicted: {prediction.PredictedLabel}");
                System.Console.WriteLine($"   Confidence: {prediction.Confidence:F2}%");

                if (prediction.AllScores.Count > 0)
                {
                    System.Console.WriteLine("\n   Top 5 predictions:");
                    var top5 = prediction.AllScores
                        .OrderByDescending(x => x.Value)
                        .Take(5);
                    foreach (var kvp in top5)
                    {
                        System.Console.WriteLine($"     {kvp.Key}: {kvp.Value:P}");
                    }
                }
            }
            else
            {
                System.Console.WriteLine("❌ Prediction failed");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }
    }
}

/// <summary>
/// Predicts characters in all images in a folder.
/// </summary>
internal static class PredictFolderCommand
{
    public static async Task ExecuteAsync(string folderPath, string modelPath)
    {
        System.Console.WriteLine($"📍 Folder: {folderPath}");
        System.Console.WriteLine($"📍 Model: {modelPath}\n");

        try
        {
            // Check if folder exists
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.Console.WriteLine($"❌ Folder not found: {folderPath}");
                return;
            }

            // Check if model exists
            if (!System.IO.File.Exists(modelPath))
            {
                System.Console.WriteLine($"❌ Model not found: {modelPath}");
                System.Console.WriteLine("   Run 'dotnet run -- train' first");
                return;
            }

            System.Console.WriteLine("🔍 Loading model...");
            var predictor = new OcrModelPredictor(modelPath);
            var loaded = await predictor.LoadModelAsync();

            if (!loaded)
            {
                System.Console.WriteLine("❌ Failed to load model");
                return;
            }

            System.Console.WriteLine("\n🔮 Making predictions...");
            var predictions = await predictor.PredictFolderAsync(folderPath);

            if (predictions.Count > 0)
            {
                System.Console.WriteLine($"\n✅ Made {predictions.Count} predictions");

                if (predictions.Count <= 10)
                {
                    System.Console.WriteLine("\n📊 Detailed Results:");
                    foreach (var pred in predictions)
                    {
                        System.Console.WriteLine($"   {pred}");
                    }
                }
                else
                {
                    var avgConfidence = predictions.Average(p => p.Confidence);
                    System.Console.WriteLine($"\n📊 Average Confidence: {avgConfidence:F2}%");
                    System.Console.WriteLine($"   Min Confidence: {predictions.Min(p => p.Confidence):F2}%");
                    System.Console.WriteLine($"   Max Confidence: {predictions.Max(p => p.Confidence):F2}%");
                }
            }
            else
            {
                System.Console.WriteLine("❌ No predictions made");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }
    }
}
