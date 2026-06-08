using OcrAlphabetTrainer.Core.Models;

namespace OcrAlphabetTrainer.Core.Services;

/// <summary>
/// Loads training data from disk, reading images from labeled folders.
/// </summary>
public class OcrDataLoader
{
    private readonly string _dataPath;

    public OcrDataLoader(string dataPath)
    {
        _dataPath = dataPath ?? throw new ArgumentNullException(nameof(dataPath));
    }

    /// <summary>
    /// Loads all training images from the data folder structure.
    /// Expects: dataPath/label/image.png
    /// </summary>
    public List<OcrImageData> LoadTrainingData()
    {
        var images = new List<OcrImageData>();

        if (!Directory.Exists(_dataPath))
        {
            throw new DirectoryNotFoundException($"Training data path not found: {_dataPath}");
        }

        var labelDirs = Directory.GetDirectories(_dataPath);
        
        foreach (var labelDir in labelDirs)
        {
            var label = Path.GetFileName(labelDir);
            var imageFiles = Directory.GetFiles(labelDir, "*.png")
                .Concat(Directory.GetFiles(labelDir, "*.jpg"))
                .Concat(Directory.GetFiles(labelDir, "*.jpeg"));

            foreach (var imageFile in imageFiles)
            {
                images.Add(new OcrImageData
                {
                    ImagePath = imageFile,
                    Label = label
                });
            }
        }

        return images;
    }

    /// <summary>
    /// Gets unique labels from training data.
    /// </summary>
    public List<string> GetLabels()
    {
        if (!Directory.Exists(_dataPath))
        {
            return new List<string>();
        }

        return Directory.GetDirectories(_dataPath)
            .Select(d => Path.GetFileName(d))
            .ToList()!;
    }

    /// <summary>
    /// Gets count of images per label.
    /// </summary>
    public Dictionary<string, int> GetLabelCounts()
    {
        var counts = new Dictionary<string, int>();

        if (!Directory.Exists(_dataPath))
        {
            return counts;
        }

        foreach (var labelDir in Directory.GetDirectories(_dataPath))
        {
            var label = Path.GetFileName(labelDir);
            var count = Directory.GetFiles(labelDir, "*.png")
                .Concat(Directory.GetFiles(labelDir, "*.jpg"))
                .Concat(Directory.GetFiles(labelDir, "*.jpeg"))
                .Count();
            counts[label!] = count;
        }

        return counts;
    }
}
