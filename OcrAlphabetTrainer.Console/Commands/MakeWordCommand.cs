using System;
using OcrAlphabetTrainer.Core.Services;

namespace OcrAlphabetTrainer.Console.Commands;

/// <summary>
/// Renders a line of uppercase text to an image, in the same style the model was trained on.
/// Useful for creating inputs to test the 'transcribe' command.
/// </summary>
internal static class MakeWordCommand
{
    public static Task ExecuteAsync(string text, string outputPath)
    {
        // Preserve case so both uppercase and lowercase letters can be rendered.
        System.Console.WriteLine($"📍 Text: {text}");
        System.Console.WriteLine($"📍 Output: {outputPath}\n");

        try
        {
            // outputPath is the destination file; the generator's folder argument is unused here.
            var generator = new TrainingImageGenerator(System.IO.Path.GetDirectoryName(outputPath) ?? ".");
            generator.GenerateWordImage(text, outputPath);

            System.Console.WriteLine($"✓ Wrote text image to: {outputPath}");
            System.Console.WriteLine($"\n📝 Try: dotnet run --project OcrAlphabetTrainer.Console -- transcribe \"{outputPath}\"");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            throw;
        }

        return Task.CompletedTask;
    }
}
