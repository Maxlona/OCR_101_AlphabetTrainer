using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
using OcrAlphabetTrainer.Core.Models;
using OcrAlphabetTrainer.Core.Services;

namespace OcrAlphabetTrainer.Model.Training;

/// <summary>
/// Trains ML.NET image classification model for OCR.
/// Uses transfer learning with a ResNet architecture (TensorFlow backend).
/// </summary>
public class OcrModelTrainer
{
    private readonly MLContext _mlContext;
    private readonly string _modelOutputPath;

    public OcrModelTrainer(string modelOutputPath)
    {
        _mlContext = new MLContext();
        _modelOutputPath = modelOutputPath ?? throw new ArgumentNullException(nameof(modelOutputPath));
    }

    /// <summary>
    /// Trains the OCR model using pixel-based feature extraction.
    /// This approach doesn't require TensorFlow and is more reliable for learning projects.
    /// </summary>
    public Task<ITransformer?> TrainModelAsync(List<OcrImageData> trainingData)
    {
        ArgumentNullException.ThrowIfNull(trainingData);

        if (trainingData.Count == 0)
        {
            System.Console.WriteLine("❌ No training data provided");
            return Task.FromResult<ITransformer?>(null);
        }

        System.Console.WriteLine($"📊 Training Data Statistics:");
        System.Console.WriteLine($"  Total images: {trainingData.Count}");

        var labelCounts = trainingData.GroupBy(x => x.Label)
            .ToDictionary(g => g.Key, g => g.Count());
        System.Console.WriteLine($"  Unique labels: {labelCounts.Count}");
        foreach (var kvp in labelCounts.OrderBy(x => x.Key))
        {
            System.Console.WriteLine($"    {Core.Services.LabelCodec.Decode(kvp.Key)}: {kvp.Value} images");
        }

        try
        {
            // Load training data as IDataView
            var imageData = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Split data: 80% train, 20% validation
            var trainTestData = _mlContext.Data.TrainTestSplit(imageData, testFraction: 0.2);
            var trainData = trainTestData.TrainSet;
            var testData = trainTestData.TestSet;

            System.Console.WriteLine("\n🤖 Building ML pipeline with ResNet transfer learning (TensorFlow)...");

            // Preprocessing: map string labels to keys and load raw image bytes.
            // The ImageClassification trainer does its own resizing/normalization internally,
            // so we feed it raw bytes (not extracted pixels).
            var preprocessingPipeline = _mlContext.Transforms.Conversion.MapValueToKey(
                    outputColumnName: "LabelKey",
                    inputColumnName: "Label")
                .Append(_mlContext.Transforms.LoadRawImageBytes(
                    outputColumnName: "Image",
                    imageFolder: "",
                    inputColumnName: nameof(OcrImageData.ImagePath)));

            // Build a preprocessed validation set for the trainer to score each epoch.
            var preprocessingTransformer = preprocessingPipeline.Fit(trainData);
            var validationImages = preprocessingTransformer.Transform(testData);

            var options = new ImageClassificationTrainer.Options
            {
                FeatureColumnName = "Image",
                LabelColumnName = "LabelKey",
                Arch = ImageClassificationTrainer.Architecture.ResnetV250,
                Epoch = 100,
                BatchSize = 16,
                LearningRate = 0.01f,
                ValidationSet = validationImages,
                MetricsCallback = metrics => System.Console.WriteLine($"  {metrics}"),
            };

            // Full pipeline: preprocessing -> ResNet transfer learning -> map predicted key back to letter.
            // Saving this full pipeline lets prediction run straight from an image path.
            var pipeline = preprocessingPipeline
                .Append(_mlContext.MulticlassClassification.Trainers.ImageClassification(options))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
                    outputColumnName: "PredictedLabel",
                    inputColumnName: "PredictedLabel"));

            System.Console.WriteLine("🚀 Training model (first run downloads the pretrained ResNet)...");

            // Train the model
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var trainedModel = pipeline.Fit(trainData);
            watch.Stop();

            System.Console.WriteLine($"✓ Model trained in {watch.Elapsed.TotalSeconds:F2} seconds");

            // Evaluate model
            System.Console.WriteLine("\n📈 Evaluating model...");
            EvaluateModel(trainedModel, testData);

            // Save model
            System.Console.WriteLine("\n💾 Saving model...");
            SaveModel(trainedModel, trainData);

            return Task.FromResult<ITransformer?>(trainedModel);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error during training: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"   Inner error: {ex.InnerException.Message}");
            }
            return Task.FromResult<ITransformer?>(null);
        }
    }

    /// <summary>
    /// Evaluates the trained model on test data.
    /// </summary>
    private void EvaluateModel(ITransformer model, IDataView testData)
    {
        try
        {
            var predictions = model.Transform(testData);
            var metrics = _mlContext.MulticlassClassification.Evaluate(
                predictions,
                labelColumnName: "LabelKey");

            System.Console.WriteLine($"  Macro Accuracy: {metrics.MacroAccuracy:P}");
            System.Console.WriteLine($"  Micro Accuracy: {metrics.MicroAccuracy:P}");
            System.Console.WriteLine($"  Log Loss: {metrics.LogLoss:F3}");
            System.Console.WriteLine($"  Log Loss Reduction: {metrics.LogLossReduction:F3}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ⚠️ Could not calculate metrics: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the trained model to disk.
    /// </summary>
    private void SaveModel(ITransformer model, IDataView trainingData)
    {
        try
        {
            var directory = Path.GetDirectoryName(_modelOutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _mlContext.Model.Save(model, trainingData.Schema, _modelOutputPath);
            System.Console.WriteLine($"  ✓ Model saved to: {_modelOutputPath}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ Error saving model: {ex.Message}");
        }
    }
}
