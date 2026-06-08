# OCR Alphabet Trainer - ML.NET Learning Project

A complete C# .NET 8 learning project for building an **Optical Character Recognition (OCR)** model using **ML.NET**. This project demonstrates machine learning fundamentals including model training, evaluation, and prediction.

## 📚 Learning Objectives

This project teaches you:

1. **Artificial Intelligence & Machine Learning Basics**
   - Understanding supervised learning
   - Image classification concepts
   - Transfer learning principles

2. **ML.NET Framework**
   - Image data loading and preprocessing
   - Model training with transfer learning
   - Model persistence and loading
   - Prediction engines

3. **Model Training & Evaluation**
   - Creating training/test data splits
   - Evaluating model accuracy
   - Understanding confidence scores
   - Improving model performance

4. **OCR (Optical Character Recognition) Concepts**
   - Image preprocessing
   - Character recognition
   - Confidence scoring
   - Single character classification (foundation for full OCR)

5. **Clean Architecture**
   - Separation of concerns
   - Service-based design
   - Dependency management
   - Testability

## 🚀 Quick Start

### Prerequisites

- **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Visual Studio 2022** or **VS Code** (optional but recommended)
- **Windows, macOS, or Linux**

### Installation

1. **Clone or extract the project**
   ```bash
   cd OcrAlphabetTrainer
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Verify the build**
   ```bash
   dotnet build
   ```

### ⚡ Quick Commands Reference

```bash
# Generate 260 synthetic training images (A-Z, 10 samples each)
dotnet run --project OcrAlphabetTrainer.Console -- generate-data

# Train ML.NET model on the generated images
dotnet run --project OcrAlphabetTrainer.Console -- train

# Predict a single image
dotnet run --project OcrAlphabetTrainer.Console -- predict "samples/test/A_001.png"

# Predict all images in a folder
dotnet run --project OcrAlphabetTrainer.Console -- predict-folder "samples/test"

# Run complete demo (generate → train → predict)
dotnet run --project OcrAlphabetTrainer.Console -- demo

# Run unit tests
dotnet test
```

## 🎯 Project Structure

```
OcrAlphabetTrainer/
├── OcrAlphabetTrainer.Console/       # Console application & commands
│   ├── Program.cs                     # Entry point & command router
│   └── Commands/                      # Command handlers
│       ├── GenerateDataCommand.cs
│       ├── TrainCommand.cs
│       ├── PredictCommand.cs
│       ├── PredictFolderCommand.cs
│       └── DemoCommand.cs
├── OcrAlphabetTrainer.Core/           # Core models & services
│   ├── Models/
│   │   ├── OcrImageData.cs           # Training data model
│   │   ├── OcrPrediction.cs          # Prediction result model
│   │   └── MlNetModels.cs            # ML.NET input/output models
│   └── Services/
│       ├── TrainingImageGenerator.cs # Generates synthetic images
│       ├── OcrDataLoader.cs          # Loads training data
│       └── OcrTextProcessor.cs       # Combines predictions into text
├── OcrAlphabetTrainer.Model/          # ML.NET model training & prediction
│   ├── Training/
│   │   └── OcrModelTrainer.cs        # Trains the model
│   └── Prediction/
│       └── OcrModelPredictor.cs      # Makes predictions
├── OcrAlphabetTrainer.Tests/          # Unit tests
├── data/                              # Training data (auto-generated)
│   └── train/
│       ├── A/
│       ├── B/
│       └── ...
├── models/                            # Trained models
│   └── ocr-alphabet-model.zip
└── samples/                           # Test samples
    └── test/
```

## 📖 Usage Guide

### How It Works - Overview

```
Training Phase:
  1. Generate synthetic training images with various fonts
  2. Organize images in label folders (A/, B/, C/, etc.)
  3. Load image paths and labels
  4. Train ML.NET model using ResNet50 transfer learning
  5. Save trained model to disk

Prediction Phase:
  1. Load trained model from disk
  2. Accept an image path
  3. Preprocess image (224×224 resize, normalize)
  4. Run through ResNet50 feature extractor
  5. Get probability distribution across 26 letters
  6. Return highest probability as prediction with confidence score
```

### 1. Generate Training Images

Generates 260 synthetic images (26 letters × 10 samples each):

```bash
dotnet run --project OcrAlphabetTrainer.Console -- generate-data
```

**Expected Output:**
```
📍 Data folder: data/train

Generating training images...
Generated character A (10 samples)
Generated character B (10 samples)
...
Generated character Z (10 samples)

✅ Training data generation complete!
   Generated 260 training images (26 letters × 10 samples)
   Images saved to: data/train
```

**Folder Structure Created:**
```
data/
└── train/
    ├── A/
    │   ├── A_001.png
    │   ├── A_002.png
    │   └── ... (10 total)
    ├── B/
    │   ├── B_001.png
    │   ├── B_002.png
    │   └── ... (10 total)
    └── Z/
        ├── Z_001.png
        ├── Z_002.png
        └── ... (10 total)
```

**Images include:**
- Different fonts (Arial, Times New Roman, Courier New, Calibri)
- Various font sizes (20-40pt)
- Random rotations (-15° to +15°)
- 64×64 pixel PNG format
- Centered black text on white background

### 2. Train the Model

Trains an ML.NET image classification model using transfer learning:

```bash
dotnet run --project OcrAlphabetTrainer.Console -- train
```

**Expected Output:**
```
📍 Data folder: data/train
📍 Model output: models/ocr-alphabet-model.zip

📂 Loading training data from 26 categories...

✓ Loaded 260 training images

📊 Label distribution:
   A: 10 images
   B: 10 images
   ...
   Z: 10 images

🎓 Training model...
Training started on 260 images
Training progress: [########## 100%]

📊 Training Results:
   Macro Accuracy: 94.23%
   Micro Accuracy: 94.23%
   Log Loss: 0.18
   Log Loss Reduction: 4.95

✅ Training complete!
   Model saved to: models/ocr-alphabet-model.zip
```

**Process:**
1. Loads all images from `data/train/` (organized in label folders)
2. Extracts label from parent folder name (A, B, C, etc.)
3. Splits data: 80% training, 20% validation
4. Applies image preprocessing (224×224 resizing, normalization)
5. Uses ResNet50 transfer learning backbone
6. Trains final classification layer for 26 letter categories
7. Evaluates on validation set
8. Saves model to `models/ocr-alphabet-model.zip`

**Output metrics:**
- **Macro Accuracy**: Average accuracy per class (each letter treated equally)
- **Micro Accuracy**: Overall accuracy across all predictions
- **Log Loss**: Cross-entropy loss (lower is better)
- **Log Loss Reduction**: How much better than baseline

### 3. Predict a Single Image

Recognizes a character in a single image:

```bash
dotnet run --project OcrAlphabetTrainer.Console -- predict "samples/test/A_001.png"
```

**Expected Output:**
```
📍 Image: samples/test/A_001.png
📍 Model: models/ocr-alphabet-model.zip

🔍 Loading model...

🔮 Making prediction...

📊 Prediction Result:
   Image: A_001.png
   Predicted: A
   Confidence: 96.52%

   Top 5 predictions:
     A: 96.52%
     Q: 1.23%
     B: 0.98%
     P: 0.87%
     R: 0.40%
```

**Confidence Levels:**
- **95-100%**: Extremely confident ✅ Trust this prediction
- **85-95%**: Very confident ✅ Likely correct
- **70-85%**: Confident ✓ Probably correct
- **50-70%**: Uncertain ⚠️ Manual review recommended
- **< 50%**: Low confidence ❌ Likely incorrect

### 4. Predict a Folder

Recognizes all characters in a folder:

```bash
dotnet run --project OcrAlphabetTrainer.Console -- predict-folder "samples/test"
```

**Expected Output:**
```
📍 Folder: samples/test
📍 Model: models/ocr-alphabet-model.zip

🔍 Loading model...

🔮 Making predictions...

✅ Made 10 predictions

📊 Detailed Results:
   samples/test/A_001.png → Predicted: A (96.52%)
   samples/test/B_001.png → Predicted: B (94.18%)
   samples/test/C_001.png → Predicted: C (97.23%)
   ...

📊 Summary Statistics:
   Average Confidence: 95.18%
   Min Confidence: 87.34%
   Max Confidence: 98.76%
```

### 5. Run Full Demo

Complete workflow: generate → train → predict

```bash
dotnet run --project OcrAlphabetTrainer.Console -- demo
```

**Executes all steps** and shows the entire process end-to-end with intermediate results.

**Expected Output:**
```
═══════════════════════════════════════════
         🎬 FULL DEMO WORKFLOW 🎬
═══════════════════════════════════════════

📍 STEP 1: Generating Training Data
────────────────────────────────────────────
[generates images]

📍 STEP 2: Training Model
────────────────────────────────────────────
[trains model and shows metrics]

📍 STEP 3: Generating Test Images
────────────────────────────────────────────
[generates sample test images]

📍 STEP 4: Making Predictions
────────────────────────────────────────────
[makes predictions on test images]

═══════════════════════════════════════════
         ✅ DEMO COMPLETE ✅
═══════════════════════════════════════════
```

### 📁 Training Data Folder Structure Requirements

**Required Organization:**

For the model to train correctly, your training data MUST be organized in this exact structure:

```
data/
└── train/                          ← Training data folder
    ├── A/                          ← Label folder (one per letter)
    │   ├── image1.png              ← Image file (any name)
    │   ├── image2.png
    │   ├── A_001.png
    │   └── ... (more images)
    ├── B/                          ← Next label folder
    │   ├── B_001.png
    │   ├── B_002.png
    │   └── ... (more images)
    ├── C/
    │   └── ... (images)
    └── Z/                          ← Final label folder
        └── ... (images)
```

**Important Rules:**

1. **Folder names = Labels**: The folder name must exactly match the character it represents
   - `A/` contains images of the letter A
   - `B/` contains images of the letter B
   - Example: ✅ `data/train/A/` | ❌ `data/train/letter_a/`

2. **One character per image**: Each image must contain exactly ONE character
   - ✅ Single letter images (64×64 pixels)
   - ❌ Multiple letters in one image

3. **Supported image formats**: PNG or JPG
   - Recommended: PNG for better quality
   - File extension must match: `.png` or `.jpg`

4. **Minimum images per character**: At least 5-10 images
   - Less data → Lower accuracy
   - More data → Better accuracy (50+ recommended for production)

5. **Recommended specifications**:
   - **Image size**: 64×64 pixels (minimum), 128×128 or higher for better quality
   - **Background**: White or light color
   - **Text**: Black or dark color
   - **Character**: Centered in image
   - **Fonts**: Variety (Arial, Times New Roman, Courier, etc.)
   - **Variations**: Different sizes, rotations (-15° to +15°), styles

**Example - Creating Custom Training Data:**

If you want to use your own images instead of synthetic ones:

```
data/
└── train/
    ├── A/
    │   ├── arial_A_10pt.png
    │   ├── times_A_12pt.png
    │   ├── courier_A_14pt.png
    │   └── custom_A.png
    ├── B/
    │   ├── arial_B_10pt.png
    │   ├── times_B_12pt.png
    │   └── courier_B_14pt.png
    └── ... (rest of letters A-Z)
```

Then train with:
```bash
dotnet run -- train
```

The `OcrDataLoader` will automatically:
1. Scan the `data/train/` folder
2. Find all label subfolders (A, B, C, ..., Z)
3. Extract all image paths from each label folder
4. Create training data pairs of (ImagePath, Label)
5. Load into ML.NET pipeline

**Verification Checklist:**

Before running `dotnet run --project OcrAlphabetTrainer.Console -- train`:

- [ ] Folder `data/train/` exists
- [ ] Subfolders A-Z exist (or at least the characters you want)
- [ ] Each subfolder contains PNG or JPG images
- [ ] Each image contains ONE character matching the folder name
- [ ] At least 5 images per character (more is better)
- [ ] No spaces or special characters in file names
- [ ] Images are not corrupted (can open them manually)

**Data Loading Code:**

The `OcrDataLoader` class handles this automatically:

```csharp
var loader = new OcrDataLoader("data/train");
var trainingData = loader.LoadTrainingData();

// trainingData is a List<OcrImageData> where each item contains:
// - ImagePath: "data/train/A/image1.png"
// - Label: "A"
```

## 🧪 Running Tests

```bash
dotnet test
```

Tests cover:
- Data loading functionality
- Prediction text processing
- Edge cases and error handling

## 📊 Understanding the Model

### Architecture

```
Image Input (224×224)
    ↓
ResNet50 Backbone (Transfer Learning)
    ↓
Feature Extraction (2048 features)
    ↓
Classification Layer
    ↓
Softmax Probability Distribution
    ↓
Predicted Label + Confidence Score
```

### Transfer Learning

This project uses **transfer learning** with ResNet50:

- **ResNet50**: Pre-trained on ImageNet with millions of images
- **Fine-tuning**: Retrains the final classification layer for our 26 letter categories
- **Advantage**: Requires fewer training images than training from scratch
- **Speed**: Much faster training compared to training a model from scratch

### Confidence Score

The confidence score is the probability that the prediction is correct:

- **90-100%**: Very confident, likely correct
- **70-90%**: Confident, probably correct
- **50-70%**: Uncertain, manual review recommended
- **< 50%**: Low confidence, likely incorrect

## 🎓 Code Examples

### Example 1: Generate Training Images

```csharp
var generator = new TrainingImageGenerator("data/train");

await generator.GenerateTrainingImagesAsync(
    characters: "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray(),
    samplesPerCharacter: 10,
    imageWidth: 64,
    imageHeight: 64,
    fontFamilies: new[] { "Arial", "Courier New" },
    useRotation: true,
    useNoise: false);
```

### Example 2: Train a Model

```csharp
var trainer = new OcrModelTrainer("models/ocr-alphabet-model.zip");
var loader = new OcrDataLoader("data/train");
var trainingData = loader.LoadTrainingData();

var model = await trainer.TrainModelAsync(trainingData);
```

### Example 3: Make Predictions

```csharp
var predictor = new OcrModelPredictor("models/ocr-alphabet-model.zip");
await predictor.LoadModelAsync();

var prediction = predictor.PredictImage("samples/test/A.png");

Console.WriteLine($"Predicted: {prediction.PredictedLabel}");
Console.WriteLine($"Confidence: {prediction.Confidence:F2}%");
```

### Example 4: Combine Multiple Predictions

```csharp
var textProcessor = new OcrTextProcessor();

var predictions = new List<OcrPrediction>
{
    new OcrPrediction { PredictedLabel = "H" },
    new OcrPrediction { PredictedLabel = "E" },
    new OcrPrediction { PredictedLabel = "L" },
    new OcrPrediction { PredictedLabel = "L" },
    new OcrPrediction { PredictedLabel = "O" }
};

var text = textProcessor.CombinePredictions(predictions);
// Result: "HELLO"
```

## 🔧 Improving Model Accuracy

### 1. More Training Data

```bash
# Modify GenerateDataCommand.cs to increase samplesPerCharacter
samplesPerCharacter: 50  // Instead of 10
```

### 2. Better Image Quality

- Use higher resolution images (128×128 instead of 64×64)
- Add more realistic variations
- Include different background styles

### 3. Augmentation

```csharp
// Increase variation with more fonts and noise
fontFamilies: new[] { "Arial", "Times New Roman", "Courier New", 
                      "Calibri", "Verdana", "Georgia" },
useNoise: true,  // Add noise patterns
```

### 4. Hyperparameter Tuning

Modify `OcrModelTrainer.cs`:
```csharp
validationSet: testData,
architecture: ImageClassificationTrainer.Architecture.ResnetV250  // Try different architectures
```

## 📚 Understanding OCR

### Current Implementation: Character-Level OCR

This project implements **single-character recognition**:
- Input: One character per image
- Output: Predicted character + confidence
- Foundation for building full OCR

### Full OCR Pipeline (Future)

Complete OCR requires additional steps:

```
Document Image
    ↓
1. Image Preprocessing (binarization, deskew)
    ↓
2. Region Detection (find text areas)
    ↓
3. Line Segmentation (split into lines)
    ↓
4. Character Segmentation (split into characters)
    ↓
5. Character Recognition (ML.NET model) ← We are here
    ↓
6. Post-processing (spell check, grammar)
    ↓
Final Text Output
```

### Key Challenges in Full OCR

- **Image Quality**: Varying quality, resolution, orientation
- **Language Complexity**: Different scripts, ligatures
- **Context**: Using language models to fix errors
- **Performance**: Real-time processing requirements
- **Edge Cases**: Handwriting, degraded text, cursive fonts

## 🚀 Future Enhancements

### Phase 2: Extended Character Support
- [ ] Add lowercase letters (a-z)
- [ ] Add digits (0-9)
- [ ] Add punctuation marks
- [ ] Support multiple languages

### Phase 3: Word-Level OCR
- [ ] Implement character segmentation
- [ ] Recognize word patterns
- [ ] Language model integration
- [ ] Dictionary-based correction

### Phase 4: Document Processing
- [ ] Multi-line text recognition
- [ ] Table detection and extraction
- [ ] Document layout analysis
- [ ] Batch processing

### Phase 5: Deployment & Integration
- [ ] REST API service
- [ ] Desktop GUI (WinUI/WPF)
- [ ] Web application (ASP.NET Core)
- [ ] **MCP Tool** for agent integration
- [ ] AI Agent integration

## 🤖 MCP & Agent Integration (Future)

This model can be exposed as an **MCP (Model Context Protocol)** tool:

```yaml
Tool Name: ocr_read_image
Input: 
  - imagePath: string (path to image)
Output:
  - extractedText: string
  - confidence: float (0-100)
  - allCharacters: { character, confidence }[]
```

### Agent Workflow Example

```
User: "Extract text from image.jpg"
    ↓
Agent: Call ocr_read_image("image.jpg")
    ↓
OCR Model: Predicts characters with confidence
    ↓
Agent: Checks confidence threshold
    ↓
If confidence < 50%:
  Agent: Request clearer image
Else:
  Agent: Return extracted text
```

## 🐛 Troubleshooting

### Issue: "Model file not found"
**Solution:** Run `dotnet run --project OcrAlphabetTrainer.Console -- generate-data` then `dotnet run --project OcrAlphabetTrainer.Console -- train` first

### Issue: "Training data not found"
**Solution:** Ensure `data/train/` directory exists and contains label folders with images

### Issue: "Out of memory" during training
**Solution:** Reduce `samplesPerCharacter` or use smaller images

### Issue: "Low accuracy (< 50%)"
**Causes & Solutions:**
- Too few training images → Generate more samples
- Images too small → Increase image resolution
- Wrong image labels → Verify folder names match characters

## 📦 NuGet Dependencies

- **Microsoft.ML** (3.0.1) - ML framework
- **Microsoft.ML.ImageAnalytics** (3.0.1) - Image processing
- **Microsoft.ML.Vision** (3.0.1) - Image classification
- **SixLabors.ImageSharp** (3.1.5) - Cross-platform image creation
- **SixLabors.Fonts** (2.0.5) - Font rendering

## 🤝 Contributing

This is a learning project. Contributions welcome! Areas to improve:

- [ ] Add data augmentation techniques
- [ ] Implement batch prediction
- [ ] Add performance benchmarks
- [ ] Create visualization tools
- [ ] Expand to other languages

## 📖 Resources

### ML.NET Documentation
- [ML.NET Official Docs](https://learn.microsoft.com/en-us/dotnet/machine-learning/)
- [Image Classification Tutorial](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/image-classification)
- [Transfer Learning Guide](https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/how-to-use-transfer-learning)

### OCR & Computer Vision
- [OpenCV Documentation](https://docs.opencv.org/)
- [OCR Fundamentals](https://en.wikipedia.org/wiki/Optical_character_recognition)
- [Deep Learning for OCR](https://arxiv.org/abs/1505.01417)

### .NET 8 Resources
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [C# Latest Features](https://learn.microsoft.com/en-us/dotnet/csharp/)

## 📝 License

This learning project is provided as-is for educational purposes.

## ❓ FAQ

**Q: Can I use this for production OCR?**
A: This is a learning project for single characters. For production, use specialized OCR libraries like Tesseract or cloud services.

**Q: How long does training take?**
A: With 260 images on a modern machine: ~5-10 seconds. Scales with data volume.

**Q: Can I improve accuracy to 99%?**
A: Possibly with more diverse training data, better images, and hyperparameter tuning.

**Q: How many training images do I need?**
A: Minimum 5-10 per character. Recommended 50+ for good accuracy.

**Q: Can I train on GPU?**
A: ML.NET supports GPU acceleration. Check the documentation for setup.

---

**Happy Learning! 🎓**

For questions or issues, check the troubleshooting section or review the source code comments.
