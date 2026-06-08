using System;
using OcrAlphabetTrainer.Core.Services;
using OcrAlphabetTrainer.Model.Training;

namespace OcrAlphabetTrainer.Console.Commands;

/// <summary>
/// Trains the ML.NET OCR model.
/// </summary>
internal static class TrainCommand
{
    public static async Task ExecuteAsync(string dataPath, string modelPath)
    {
        System.Console.WriteLine($"📍 Data folder: {dataPath}");
        System.Console.WriteLine($"📍 Model output: {modelPath}\n");

        try
        {
            // Check if training data exists
            if (!System.IO.Directory.Exists(dataPath))
            {
                System.Console.WriteLine($"❌ Training data not found at: {dataPath}");
                System.Console.WriteLine("   Run 'dotnet run -- generate-data' first");
                return;
            }

            var labelDirs = System.IO.Directory.GetDirectories(dataPath);
            if (labelDirs.Length == 0)
            {
                System.Console.WriteLine($"❌ No training data found in: {dataPath}");
                System.Console.WriteLine("   Run 'dotnet run -- generate-data' first");
                return;
            }

            // Load training data
            System.Console.WriteLine($"📂 Loading training data from {labelDirs.Length} categories...\n");
            var dataLoader = new OcrDataLoader(dataPath);
            var trainingData = dataLoader.LoadTrainingData();

            if (trainingData.Count == 0)
            {
                System.Console.WriteLine("❌ No training images found");
                return;
            }

            System.Console.WriteLine($"✓ Loaded {trainingData.Count} training images\n");

            // Display label statistics
            var labelCounts = dataLoader.GetLabelCounts();
            System.Console.WriteLine("📊 Label distribution:");
            foreach (var kvp in labelCounts.OrderBy(x => x.Key))
            {
                System.Console.WriteLine($"   {LabelCodec.Decode(kvp.Key)}: {kvp.Value} images");
            }
            System.Console.WriteLine();

            // Train model
            var trainer = new OcrModelTrainer(modelPath);
            var model = await trainer.TrainModelAsync(trainingData);

            if (model != null)
            {
                System.Console.WriteLine("\n✅ Training complete!");
                System.Console.WriteLine($"   Model saved to: {modelPath}");
                System.Console.WriteLine("\n📝 Next steps:");
                System.Console.WriteLine("   - Test with: dotnet run -- predict samples/test/A_test.png");
                System.Console.WriteLine("   - Run demo:  dotnet run -- demo");
            }
            else
            {
                System.Console.WriteLine("\n❌ Training failed");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }
    }
}
