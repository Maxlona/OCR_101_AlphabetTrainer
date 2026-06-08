using OcrAlphabetTrainer.Core.Models;
using OcrAlphabetTrainer.Core.Services;
using Xunit;

namespace OcrAlphabetTrainer.Tests;

public class OcrDataLoaderTests
{
    private readonly string _testDataPath = Path.Combine(
        Path.GetTempPath(),
        "OcrTestData");

    public OcrDataLoaderTests()
    {
        // Clean up any previous test data
        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
    }

    [Fact]
    public void LoadTrainingData_WithNoData_ReturnsEmptyList()
    {
        // Arrange
        Directory.CreateDirectory(_testDataPath);
        var loader = new OcrDataLoader(_testDataPath);

        // Act
        var data = loader.LoadTrainingData();

        // Assert
        Assert.Empty(data);
    }

    [Fact]
    public void LoadTrainingData_WithValidStructure_ReturnsCorrectData()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDataPath, "A"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "B"));

        // Create dummy files
        File.WriteAllText(Path.Combine(_testDataPath, "A", "image1.png"), "dummy");
        File.WriteAllText(Path.Combine(_testDataPath, "B", "image1.png"), "dummy");

        var loader = new OcrDataLoader(_testDataPath);

        // Act
        var data = loader.LoadTrainingData();

        // Assert
        Assert.Equal(2, data.Count);
        Assert.Single(data.Where(d => d.Label == "A"));
        Assert.Single(data.Where(d => d.Label == "B"));
    }

    [Fact]
    public void GetLabels_ReturnsAllLabelDirectories()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDataPath, "A"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "B"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "C"));

        var loader = new OcrDataLoader(_testDataPath);

        // Act
        var labels = loader.GetLabels();

        // Assert
        Assert.Equal(3, labels.Count);
        Assert.Contains("A", labels);
        Assert.Contains("B", labels);
        Assert.Contains("C", labels);
    }

    [Fact]
    public void GetLabelCounts_ReturnsCorrectCounts()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDataPath, "A"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "B"));

        File.WriteAllText(Path.Combine(_testDataPath, "A", "image1.png"), "dummy");
        File.WriteAllText(Path.Combine(_testDataPath, "A", "image2.png"), "dummy");
        File.WriteAllText(Path.Combine(_testDataPath, "B", "image1.png"), "dummy");

        var loader = new OcrDataLoader(_testDataPath);

        // Act
        var counts = loader.GetLabelCounts();

        // Assert
        Assert.Equal(2, counts["A"]);
        Assert.Equal(1, counts["B"]);
    }

    [Fact]
    public void LoadTrainingData_WithNonExistentPath_ThrowsException()
    {
        // Arrange
        var loader = new OcrDataLoader("/nonexistent/path");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => loader.LoadTrainingData());
    }
}
