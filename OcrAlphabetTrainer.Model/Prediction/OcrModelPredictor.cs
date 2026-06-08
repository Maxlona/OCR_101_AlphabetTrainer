using Microsoft.ML;
using Microsoft.ML.Data;
using OcrAlphabetTrainer.Core.Models;
using OcrAlphabetTrainer.Core.Services;

namespace OcrAlphabetTrainer.Model.Prediction;

/// <summary>
/// Loads a trained model and makes predictions on new images.
/// </summary>
public class OcrModelPredictor
{
    private readonly MLContext _mlContext;
    private readonly string _modelPath;
    private ITransformer? _model;
    private PredictionEngine<ImageInputData, ImagePredictionData>? _predictionEngine;

    public OcrModelPredictor(string modelPath)
    {
        _mlContext = new MLContext();
        _modelPath = modelPath ?? throw new ArgumentNullException(nameof(modelPath));
    }

    /// <summary>
    /// Loads the model from disk.
    /// </summary>
    public async Task<bool> LoadModelAsync()
    {
        try
        {
            if (!File.Exists(_modelPath))
            {
                Console.WriteLine($"❌ Model file not found: {_modelPath}");
                return false;
            }

            Console.WriteLine($"📥 Loading model from: {_modelPath}");

            using (var stream = File.OpenRead(_modelPath))
            {
                _model = _mlContext.Model.Load(stream, out var schema);
            }

            if (_model == null)
            {
                Console.WriteLine("❌ Failed to load model");
                return false;
            }

            // Create prediction engine
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ImageInputData, ImagePredictionData>(_model);

            Console.WriteLine("✓ Model loaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading model: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Predicts the character in an image.
    /// </summary>
    public OcrPrediction? PredictImage(string imagePath)
    {
        if (_predictionEngine == null)
        {
            Console.WriteLine("❌ Model not loaded. Call LoadModelAsync first.");
            return null;
        }

        try
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"❌ Image not found: {imagePath}");
                return null;
            }

            var input = new ImageInputData { ImagePath = imagePath };
            var prediction = _predictionEngine.Predict(input);

            // Map each score index back to its letter using the Score column's slot names
            var labels = new List<string>();
            VBuffer<ReadOnlyMemory<char>> slotNames = default;
            _predictionEngine.OutputSchema["Score"].GetSlotNames(ref slotNames);
            foreach (var name in slotNames.DenseValues())
            {
                labels.Add(name.ToString());
            }

            var allScores = new Dictionary<string, float>();
            for (int i = 0; i < prediction.Score.Length; i++)
            {
                // Decode case-safe labels (e.g. "U_A" / "L_a") back to the real character.
                var name = i < labels.Count ? LabelCodec.Decode(labels[i]) : i.ToString();
                allScores[name] = prediction.Score[i];
            }

            var maxScore = prediction.Score.Length > 0 ? prediction.Score.Max() : 0f;

            return new OcrPrediction
            {
                PredictedLabel = LabelCodec.Decode(prediction.PredictedLabel),
                Score = maxScore,
                Confidence = maxScore * 100f,
                AllScores = allScores
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error predicting image: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Predicts characters in a folder of images.
    /// </summary>
    public async Task<List<OcrPrediction>> PredictFolderAsync(string folderPath)
    {
        var predictions = new List<OcrPrediction>();

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"❌ Folder not found: {folderPath}");
            return predictions;
        }

        var imageFiles = Directory.GetFiles(folderPath, "*.png")
            .Concat(Directory.GetFiles(folderPath, "*.jpg"))
            .Concat(Directory.GetFiles(folderPath, "*.jpeg"))
            .ToList();

        Console.WriteLine($"🖼️ Predicting {imageFiles.Count} images from {folderPath}...");

        foreach (var imageFile in imageFiles)
        {
            var prediction = PredictImage(imageFile);
            if (prediction != null)
            {
                predictions.Add(prediction);
                Console.WriteLine($"  {Path.GetFileName(imageFile)}: {prediction}");
            }
        }

        return predictions;
    }

    /// <summary>
    /// Checks if model is loaded.
    /// </summary>
    public bool IsModelLoaded => _model != null && _predictionEngine != null;
}
