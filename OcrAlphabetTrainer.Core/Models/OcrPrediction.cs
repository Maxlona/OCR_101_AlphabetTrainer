namespace OcrAlphabetTrainer.Core.Models;

/// <summary>
/// Represents the prediction result for an OCR image.
/// </summary>
public class OcrPrediction
{
    /// <summary>
    /// The predicted character/label.
    /// </summary>
    public string PredictedLabel { get; set; } = string.Empty;

    /// <summary>
    /// Raw score from the ML model.
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Confidence level as a percentage (0-100).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// All scores for each label.
    /// </summary>
    public Dictionary<string, float> AllScores { get; set; } = new();

    public override string ToString()
    {
        return $"Label: {PredictedLabel}, Confidence: {Confidence:F2}%";
    }
}
