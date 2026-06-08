using System;
using System.IO;
using System.Collections.Generic;
using OcrAlphabetTrainer.Console.Commands;

namespace OcrAlphabetTrainer.Console;

class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("╔════════════════════════════════════════════╗");
        System.Console.WriteLine("║  OCR Alphabet Trainer - ML.NET Edition     ║");
        System.Console.WriteLine("║  Learning AI, ML, and Model Training       ║");
        System.Console.WriteLine("╚════════════════════════════════════════════╝\n");

        // Get base paths
        var solutionRoot = GetSolutionRoot();
        var dataPath = Path.Combine(solutionRoot, "data", "train");
        var modelPath = Path.Combine(solutionRoot, "models", "ocr-alphabet-model.zip");
        var samplesPath = Path.Combine(solutionRoot, "samples", "test");

        try
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var command = args[0].ToLower();

            switch (command)
            {
                case "generate-data":
                    await HandleGenerateData(dataPath);
                    break;

                case "train":
                    await HandleTrain(dataPath, modelPath);
                    break;

                case "predict":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine("❌ Usage: dotnet run --project OcrAlphabetTrainer.Console -- predict <image-path>");
                        return;
                    }
                    await HandlePredict(args[1], modelPath);
                    break;

                case "make-word":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine("❌ Usage: dotnet run --project OcrAlphabetTrainer.Console -- make-word <TEXT> [output-path]");
                        return;
                    }
                    var wordOut = args.Length >= 3 ? args[2] : Path.Combine(samplesPath, "word.png");
                    await MakeWordCommand.ExecuteAsync(args[1], wordOut);
                    break;

                case "transcribe":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine("❌ Usage: dotnet run --project OcrAlphabetTrainer.Console -- transcribe <image-path>");
                        return;
                    }
                    await HandleTranscribe(args[1], modelPath);
                    break;

                case "predict-folder":
                    if (args.Length < 2)
                    {
                        System.Console.WriteLine("❌ Usage: dotnet run --project OcrAlphabetTrainer.Console -- predict-folder <folder-path>");
                        return;
                    }
                    await HandlePredictFolder(args[1], modelPath);
                    break;

                case "demo":
                    await HandleDemo(dataPath, modelPath, samplesPath);
                    break;

                default:
                    System.Console.WriteLine($"❌ Unknown command: {command}");
                    ShowHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"\n❌ Error: {ex.Message}");
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    static async Task HandleGenerateData(string dataPath)
    {
        System.Console.WriteLine("🎨 Generating Training Data\n");
        await GenerateDataCommand.ExecuteAsync(dataPath);
    }

    static async Task HandleTrain(string dataPath, string modelPath)
    {
        System.Console.WriteLine("🤖 Training Model\n");
        await TrainCommand.ExecuteAsync(dataPath, modelPath);
    }

    static async Task HandlePredict(string imagePath, string modelPath)
    {
        System.Console.WriteLine("🔍 Predicting Image\n");
        await PredictCommand.ExecuteAsync(imagePath, modelPath);
    }

    static async Task HandleTranscribe(string imagePath, string modelPath)
    {
        System.Console.WriteLine("📝 Transcribing Image to Text\n");
        await TranscribeCommand.ExecuteAsync(imagePath, modelPath);
    }

    static async Task HandlePredictFolder(string folderPath, string modelPath)
    {
        System.Console.WriteLine("📁 Predicting Folder\n");
        await PredictFolderCommand.ExecuteAsync(folderPath, modelPath);
    }

    static async Task HandleDemo(string dataPath, string modelPath, string samplesPath)
    {
        System.Console.WriteLine("🎬 Running Full Demo\n");
        await DemoCommand.ExecuteAsync(dataPath, modelPath, samplesPath);
    }

    static string GetSolutionRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        // Walk up until we find the solution file or reach root
        while (!File.Exists(Path.Combine(currentDir, "OcrAlphabetTrainer.sln")))
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null)
                break;
            currentDir = parent.FullName;
        }

        return currentDir;
    }

    static void ShowHelp()
    {
        System.Console.WriteLine("Available commands:\n");
        System.Console.WriteLine("  generate-data       Generate synthetic training images");
        System.Console.WriteLine("  train               Train the ML.NET model");
        System.Console.WriteLine("  predict <path>      Predict a single image");
        System.Console.WriteLine("  transcribe <path>   Transcribe an image of text to a string");
        System.Console.WriteLine("  make-word <TEXT>    Render uppercase text to an image (test input for transcribe)");
        System.Console.WriteLine("  predict-folder <path>  Predict all images in a folder");
        System.Console.WriteLine("  demo                Run full demo (generate, train, predict)");
        System.Console.WriteLine("\nExamples:");
        System.Console.WriteLine("  dotnet run --project OcrAlphabetTrainer.Console -- generate-data");
        System.Console.WriteLine("  dotnet run --project OcrAlphabetTrainer.Console -- train");
        System.Console.WriteLine("  dotnet run --project OcrAlphabetTrainer.Console -- predict samples/test/A_test.png");
        System.Console.WriteLine("  dotnet run --project OcrAlphabetTrainer.Console -- demo");
    }
}

