using OcrAlphabetTrainer.Core.Models;
using OcrAlphabetTrainer.Core.Services;
using Xunit;

namespace OcrAlphabetTrainer.Tests;

public class OcrTextProcessorTests
{
    private readonly OcrTextProcessor _processor = new();

    [Fact]
    public void CombinePredictions_WithValidPredictions_ReturnsCombinedText()
    {
        // Arrange
        var predictions = new List<OcrPrediction>
        {
            new OcrPrediction { PredictedLabel = "H", Confidence = 95 },
            new OcrPrediction { PredictedLabel = "E", Confidence = 92 },
            new OcrPrediction { PredictedLabel = "L", Confidence = 88 },
            new OcrPrediction { PredictedLabel = "L", Confidence = 89 },
            new OcrPrediction { PredictedLabel = "O", Confidence = 94 }
        };

        // Act
        var result = _processor.CombinePredictions(predictions);

        // Assert
        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void CombinePredictions_WithEmptyList_ReturnsEmptyString()
    {
        // Arrange
        var predictions = new List<OcrPrediction>();

        // Act
        var result = _processor.CombinePredictions(predictions);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CombinePredictionsWithConfidence_FiltersByThreshold()
    {
        // Arrange
        var predictions = new List<OcrPrediction>
        {
            new OcrPrediction { PredictedLabel = "A", Confidence = 95 },
            new OcrPrediction { PredictedLabel = "B", Confidence = 50 },  // Below 60% threshold
            new OcrPrediction { PredictedLabel = "C", Confidence = 70 }
        };

        // Act
        var result = _processor.CombinePredictionsWithConfidence(predictions, 0.6f);

        // Assert
        Assert.Equal("AC", result);
    }

    [Fact]
    public void GetAverageConfidence_CalculatesCorrectly()
    {
        // Arrange
        var predictions = new List<OcrPrediction>
        {
            new OcrPrediction { Confidence = 100 },
            new OcrPrediction { Confidence = 80 },
            new OcrPrediction { Confidence = 60 }
        };

        // Act
        var average = _processor.GetAverageConfidence(predictions);

        // Assert
        Assert.Equal(0.8f, average, 0.01f);
    }

    [Fact]
    public void GetMinimumConfidence_ReturnsLowestValue()
    {
        // Arrange
        var predictions = new List<OcrPrediction>
        {
            new OcrPrediction { Confidence = 95 },
            new OcrPrediction { Confidence = 60 },
            new OcrPrediction { Confidence = 85 }
        };

        // Act
        var minimum = _processor.GetMinimumConfidence(predictions);

        // Assert
        Assert.Equal(0.6f, minimum);
    }

    [Fact]
    public void OcrPredictionToString_FormatsCorrectly()
    {
        // Arrange
        var prediction = new OcrPrediction
        {
            PredictedLabel = "A",
            Confidence = 95.5f
        };

        // Act
        var result = prediction.ToString();

        // Assert
        Assert.Contains("A", result);
        Assert.Contains("95.50%", result);
    }
}
