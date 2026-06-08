using Microsoft.ML.Data;

namespace OcrAlphabetTrainer.Core.Models;

/// <summary>
/// ML.NET input class for image classification.
/// </summary>
public class ImageInputData
{
    [LoadColumn(0)]
    public string ImagePath { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// ML.NET output class for image classification predictions.
/// </summary>
public class ImagePredictionData
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;

    [ColumnName("Score")]
    public float[] Score { get; set; } = [];
}
