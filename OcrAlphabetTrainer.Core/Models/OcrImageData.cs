namespace OcrAlphabetTrainer.Core.Models;

/// <summary>
/// Represents an image with its associated label (character) for training.
/// </summary>
public class OcrImageData
{
    /// <summary>
    /// Full path to the image file.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Label for the image (the character it represents).
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
