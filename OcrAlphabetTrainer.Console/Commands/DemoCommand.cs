using System;
using OcrAlphabetTrainer.Core.Services;
using OcrAlphabetTrainer.Model.Training;
using OcrAlphabetTrainer.Model.Prediction;

namespace OcrAlphabetTrainer.Console.Commands;

/// <summary>
/// Runs a complete demo: generate data, train model, and make predictions.
/// </summary>
internal static class DemoCommand
{
    public static async Task ExecuteAsync(string dataPath, string modelPath, string samplesPath)
    {
        System.Console.WriteLine("═══════════════════════════════════════════");
        System.Console.WriteLine("         🎬 FULL DEMO WORKFLOW 🎬");
        System.Console.WriteLine("═══════════════════════════════════════════\n");

        try
        {
            // Step 1: Generate data
            System.Console.WriteLine("\n📍 STEP 1: Generating Training Data\n");
            System.Console.WriteLine("────────────────────────────────────────────");
            await GenerateDataCommand.ExecuteAsync(dataPath);

            // Step 2: Train model
            System.Console.WriteLine("\n\n📍 STEP 2: Training Model\n");
            System.Console.WriteLine("────────────────────────────────────────────");
            await TrainCommand.ExecuteAsync(dataPath, modelPath);

            // Step 3: Generate sample test images
            System.Console.WriteLine("\n\n📍 STEP 3: Generating Test Images\n");
            System.Console.WriteLine("────────────────────────────────────────────");
            await GenerateSampleTestImages(samplesPath);

            // Step 4: Make predictions
            if (System.IO.Directory.Exists(samplesPath))
            {
                var testImages = System.IO.Directory.GetFiles(samplesPath, "*.png")
                    .Concat(System.IO.Directory.GetFiles(samplesPath, "*.jpg"))
                    .ToList();

                if (testImages.Count > 0)
                {
                    System.Console.WriteLine("\n\n📍 STEP 4: Making Predictions\n");
                    System.Console.WriteLine("────────────────────────────────────────────");
                    await PredictFolderCommand.ExecuteAsync(samplesPath, modelPath);
                }
            }

            System.Console.WriteLine("\n\n═══════════════════════════════════════════");
            System.Console.WriteLine("         ✅ DEMO COMPLETE ✅");
            System.Console.WriteLine("═══════════════════════════════════════════\n");

            System.Console.WriteLine("📚 What you learned:");
            System.Console.WriteLine("   • How to generate synthetic training images");
            System.Console.WriteLine("   • How to train an ML.NET image classification model");
            System.Console.WriteLine("   • How to load a model and make predictions");
            System.Console.WriteLine("   • How to evaluate model confidence");

            System.Console.WriteLine("\n🚀 Next steps:");
            System.Console.WriteLine("   • Explore the source code in each project");
            System.Console.WriteLine("   • Try with your own images");
            System.Console.WriteLine("   • Modify training parameters to improve accuracy");
            System.Console.WriteLine("   • Extend to word-level OCR with character segmentation");

            System.Console.WriteLine("\n💡 Future enhancements:");
            System.Console.WriteLine("   • Add lowercase letters (a-z)");
            System.Console.WriteLine("   • Add digits (0-9)");
            System.Console.WriteLine("   • Implement character segmentation for words");
            System.Console.WriteLine("   • Create an MCP tool for agent integration");
            System.Console.WriteLine("   • Build a desktop GUI with WinUI or WPF");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"\n❌ Demo failed: {ex.Message}");
            throw;
        }
    }

    static async Task GenerateSampleTestImages(string samplesPath)
    {
        System.IO.Directory.CreateDirectory(samplesPath);

        System.Console.WriteLine($"Generating sample test images to: {samplesPath}\n");

        var generator = new TrainingImageGenerator(samplesPath);

        // Generate just a few test samples (A, B, C)
        var testCharacters = new[] { 'A', 'B', 'C' };

        await generator.GenerateTrainingImagesAsync(
            characters: testCharacters,
            samplesPerCharacter: 2,
            imageWidth: 64,
            imageHeight: 64,
            fontFamilies: new[] { "Arial", "Courier New" },
            useRotation: true,
            useNoise: false);

        System.Console.WriteLine($"✓ Generated {testCharacters.Length * 2} test images");
    }
}
