using System;
using OcrAlphabetTrainer.Core.Services;

namespace OcrAlphabetTrainer.Console.Commands;

/// <summary>
/// Generates synthetic training images for OCR model training.
/// </summary>
internal static class GenerateDataCommand
{
    public static async Task ExecuteAsync(string dataPath)
    {
        System.Console.WriteLine($"📍 Data folder: {dataPath}\n");

        try
        {
            // Create data directory if it doesn't exist
            System.IO.Directory.CreateDirectory(dataPath);

            // Define uppercase (A-Z), lowercase (a-z), and common punctuation so the model learns all.
            var characters = (
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz" +
                ".,!?:;'\"-()").ToCharArray();

            var generator = new TrainingImageGenerator(dataPath);

            // Generate training images
            const int samplesPerCharacter = 50;

            await generator.GenerateTrainingImagesAsync(
                characters: characters,
                samplesPerCharacter: samplesPerCharacter,
                imageWidth: 64,
                imageHeight: 64,
                fontFamilies: new[] { "Arial", "Times New Roman", "Courier New", "Calibri" },
                useRotation: true,
                useNoise: false);

            System.Console.WriteLine("\n✅ Training data generation complete!");
            System.Console.WriteLine($"   Generated {characters.Length * samplesPerCharacter} training images ({characters.Length} characters × {samplesPerCharacter} samples)");
            System.Console.WriteLine($"   Images saved to: {dataPath}");
            System.Console.WriteLine("\n📝 Next step: Run 'dotnet run -- train' to train the model");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }
    }
}
