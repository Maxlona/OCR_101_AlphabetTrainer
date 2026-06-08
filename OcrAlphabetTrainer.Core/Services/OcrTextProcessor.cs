using OcrAlphabetTrainer.Core.Models;

namespace OcrAlphabetTrainer.Core.Services;

/// <summary>
/// Processes multiple character predictions to build text strings.
/// This is for future use when transitioning from character-level to word/sentence-level OCR.
/// </summary>
public class OcrTextProcessor
{
    /// <summary>
    /// Combines multiple character predictions into text.
    /// </summary>
    public string CombinePredictions(IEnumerable<OcrPrediction> predictions)
    {
        return string.Concat(predictions.Select(p => p.PredictedLabel));
    }

    /// <summary>
    /// Combines predictions and filters by confidence threshold.
    /// </summary>
    public string CombinePredictionsWithConfidence(
        IEnumerable<OcrPrediction> predictions,
        float confidenceThreshold = 0.5f)
    {
        var filteredPredictions = predictions
            .Where(p => p.Confidence / 100f >= confidenceThreshold)
            .Select(p => p.PredictedLabel);

        return string.Concat(filteredPredictions);
    }

    /// <summary>
    /// Gets average confidence across predictions.
    /// </summary>
    public float GetAverageConfidence(IEnumerable<OcrPrediction> predictions)
    {
        var predictionList = predictions.ToList();
        if (predictionList.Count == 0)
            return 0;

        return predictionList.Average(p => p.Confidence) / 100f;
    }

    /// <summary>
    /// Gets the minimum confidence from all predictions.
    /// </summary>
    public float GetMinimumConfidence(IEnumerable<OcrPrediction> predictions)
    {
        return predictions.Min(p => p.Confidence) / 100f;
    }
}
