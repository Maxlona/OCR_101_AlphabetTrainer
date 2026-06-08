# OCR Alphabet Trainer - ML.NET Learning Project

A C# .NET 8 learning project that builds an **Optical Character Recognition (OCR)** system with **ML.NET**. It trains a neural-network character classifier (ResNet transfer learning on a TensorFlow backend) and pairs it with a classical image-processing segmenter to **transcribe a single line of text from an image to a string**.

The model recognizes **63 characters**: uppercase `A–Z`, lowercase `a–z`, and common punctuation `. , ! ? : ; ' " - ( )`.

---

## 📚 What This Project Teaches

1. **Transfer learning with a real neural network** — using a pretrained **ResNet V2-50** CNN (≈25.5M parameters) and training only a small classification head (≈129K parameters) for our characters.
2. **The ML.NET image-classification pipeline** — `LoadRawImageBytes → ImageClassification (ResNet/TensorFlow) → MapKeyToValue`, plus model save/load and prediction engines.
3. **A hybrid OCR pipeline** — classical image processing (segmentation, baseline detection, normalization) feeding a neural classifier.
4. **Practical data-engineering details** — case-safe & filename-safe label encoding, baseline-relative glyph normalization, and why they matter.
5. **Honest model limitations** — what a single-line glyph classifier can and cannot do, and where you'd reach for a full OCR engine instead.

---

## 🧠 How It Works (the big picture)

`transcribe` runs a **two-stage hybrid pipeline**:

```
Input image (one line of text)
        │
        ▼
[1] CharacterSegmenter        ← classical image processing (NO neural net)
    • grayscale + ink threshold
    • vertical projection → find each character's columns
    • bounding boxes, baseline & cap-height estimation
    • split over-wide blobs (touching/bold letters)
        │  (one cropped glyph per character)
        ▼
[2] GlyphNormalizer           ← places each glyph on a 64×64 canvas,
    • baseline-relative position, size preserved   keeping size & vertical position
        │
        ▼
[3] OcrModelPredictor → ResNet V2-50 (TensorFlow)   ← the neural network READS each glyph
        │  (predicted character + confidence)
        ▼
[4] TranscribeCommand → assembles characters + spaces → prints the text
```

- **Image processing / "where are the characters?"** → `CharacterSegmenter` + `GlyphNormalizer` (plain pixel math).
- **Recognition / "what character is this?"** → the **ResNet neural network**.

---

## 🚀 Quick Start

### Prerequisites
- **.NET 8 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Windows / macOS / Linux** (x64 — required by the TensorFlow native runtime)
- Internet access on the **first** training run (downloads the pretrained ResNet)

### Build
```bash
cd OcrAlphabetTrainer
dotnet restore
dotnet build
```

> **⚠️ Run commands from the `OcrAlphabetTrainer` folder** (the one containing `OcrAlphabetTrainer.sln`). The app locates its `data/`, `models/`, and `samples/` folders by walking up to the `.sln`, so running from elsewhere will read/write the wrong locations. See [Troubleshooting](#-troubleshooting).

### ⚡ Commands

```bash
# 1. Generate synthetic training images (63 chars × 50 samples = 3150 images)
dotnet run --project OcrAlphabetTrainer.Console -- generate-data

# 2. Train the ResNet model (first run downloads the pretrained network)
dotnet run --project OcrAlphabetTrainer.Console -- train

# 3a. Classify a single-character image (verbose: top-5 + confidence)
dotnet run --project OcrAlphabetTrainer.Console -- predict "samples/test/word.png"

# 3b. Transcribe a single LINE of text to a string  ← the headline feature
dotnet run --project OcrAlphabetTrainer.Console -- transcribe "samples/test/word.png"

# Helper: render a line of text to an image (test input for `transcribe`)
dotnet run --project OcrAlphabetTrainer.Console -- make-word "Hello World" "samples/test/word.png"

# Classify every image in a folder
dotnet run --project OcrAlphabetTrainer.Console -- predict-folder "samples/test"

# End-to-end demo (generate → train → predict)
dotnet run --project OcrAlphabetTrainer.Console -- demo
```

A typical workflow to try transcription:
```bash
dotnet run --project OcrAlphabetTrainer.Console -- generate-data
dotnet run --project OcrAlphabetTrainer.Console -- train
dotnet run --project OcrAlphabetTrainer.Console -- make-word "Hello, World!" "samples/test/hi.png"
dotnet run --project OcrAlphabetTrainer.Console -- transcribe "samples/test/hi.png"
# → 📝 Transcribed text:  Hello, World!
```

---

## 🎯 Project Structure

```
OcrAlphabetTrainer/
├── OcrAlphabetTrainer.Console/        # Console app & command handlers
│   ├── Program.cs                     # Entry point & command router
│   └── Commands/
│       ├── GenerateDataCommand.cs     # Synthesize training images
│       ├── TrainCommand.cs            # Train the model
│       ├── PredictCommand.cs          # Classify one image (+ PredictFolderCommand)
│       ├── TranscribeCommand.cs       # Segment a line → classify → print text  ★ new
│       ├── MakeWordCommand.cs         # Render text → image (test input)         ★ new
│       └── DemoCommand.cs             # End-to-end demo
├── OcrAlphabetTrainer.Core/           # Models & services
│   ├── Models/
│   │   ├── OcrImageData.cs            # (ImagePath, Label) training row
│   │   ├── OcrPrediction.cs           # Prediction result
│   │   └── MlNetModels.cs             # ML.NET input/output schemas
│   └── Services/
│       ├── TrainingImageGenerator.cs  # Generates glyph & word images (baseline-aware)
│       ├── CharacterSegmenter.cs      # Line → individual glyphs                  ★ new
│       ├── GlyphNormalizer.cs         # Shared baseline-relative canvas placement ★ new
│       ├── LabelCodec.cs              # Case-/filename-safe label encoding        ★ new
│       ├── OcrDataLoader.cs           # Loads labeled images from folders
│       └── OcrTextProcessor.cs        # Combines predictions into text
├── OcrAlphabetTrainer.Model/          # ML.NET training & prediction
│   ├── Training/OcrModelTrainer.cs    # Builds & fits the ResNet pipeline
│   └── Prediction/OcrModelPredictor.cs# Runs the model on a glyph
├── data/train/                        # Auto-generated training data (encoded folders)
├── models/ocr-alphabet-model.zip      # Trained model (full pipeline, saved)
└── samples/test/                      # Test images
```

---

## 🔤 Characters & Label Encoding

The model is trained on **63 classes**:

| Group | Characters | Count |
|-------|-----------|-------|
| Uppercase | `A`–`Z` | 26 |
| Lowercase | `a`–`z` | 26 |
| Punctuation | `. , ! ? : ; ' " - ( )` | 11 |

### Why folder names look like `U_A`, `L_a`, `P_period`

The training label **is the folder name**, but raw character folders don't work:

1. **Windows filesystems are case-insensitive** — a folder `A` and a folder `a` are the *same* directory, which would silently merge `A` and `a` into one class.
2. **Many punctuation marks are illegal in filenames** (`? : " * < > | / \`) and `.` is a reserved name.

`LabelCodec` solves both by mapping every character to a safe folder name and decoding it back at prediction time:

| Character | Folder name |
|-----------|-------------|
| `A` | `U_A` |
| `a` | `L_a` |
| `.` | `P_period` |
| `,` | `P_comma` |
| `!` | `P_exclaim` |
| `?` | `P_question` |
| `'` | `P_apostrophe` |
| `"` | `P_quote` |
| ... | ... |

So `data/train/` contains folders like `U_A … U_Z`, `L_a … L_z`, `P_period`, `P_comma`, etc. — each with 50 images.

---

## 📖 Usage Details

### 1. Generate Training Images

```bash
dotnet run --project OcrAlphabetTrainer.Console -- generate-data
```

Creates **3150 images** (63 characters × 50 samples). Each glyph is rendered in a random training font (Arial, Times New Roman, Courier New, Calibri) at a random size, then **normalized onto a 64×64 canvas relative to the text baseline** — so a lowercase `o` stays smaller than capital `O`, a period sits low, and an apostrophe sits high. Preserving size & vertical position is what makes case and punctuation distinguishable.

### 2. Train the Model

```bash
dotnet run --project OcrAlphabetTrainer.Console -- train
```

- Loads images, maps labels to keys (`MapValueToKey`), loads raw image bytes.
- Trains with **`ImageClassification` (ResNet V2-50)** on the **TensorFlow** backend; an 80/20 train/validation split is reported per epoch.
- Saves the **full pipeline** (preprocessing → ResNet → label decode) to `models/ocr-alphabet-model.zip`, so prediction runs straight from an image path.

Typical result on the synthetic data: **~99% validation macro-accuracy**. First run is slower (downloads the pretrained ResNet); later runs reuse the cached network.

### 3. Transcribe a Line of Text

```bash
dotnet run --project OcrAlphabetTrainer.Console -- transcribe "samples/test/word.png"
```

Segments the image into characters, classifies each, and prints the assembled string with per-character confidence. Best results come from a **single line** of cleanly-spaced text in a trained-style font.

### 4. Make a Test Image

```bash
dotnet run --project OcrAlphabetTrainer.Console -- make-word "OCR is fun!" "samples/test/demo.png"
```

Renders the text on a single baseline with clear letter spacing — an easy way to create inputs for `transcribe`.

### 5. Predict a Single Glyph / Folder

`predict` and `predict-folder` classify pre-cropped single-character images and report confidence and the top-5 candidates.

---

## 📊 The Model

### Architecture (transfer learning)

```
64×64 glyph
    ↓  (resized to 224×224 internally)
ResNet V2-50 backbone   ← pretrained on ImageNet, FROZEN  (~25.5M params)
    ↓
2048-dim feature vector
    ↓
Classification head     ← the only part WE train  (2048×63 + 63 ≈ 129K params)
    ↓
Softmax over 63 classes → predicted character + confidence
```

| | Parameters | Trained here? |
|---|---|---|
| ResNet-50 feature extractor | ~25.5 M | ❌ pretrained, frozen |
| Classification head (2048→63) | ~129 K | ✅ yes |

Only ~0.5% of the network's parameters are trained — the essence of transfer learning.

### Metrics reported after training
- **Macro Accuracy** — average accuracy per class (each character weighted equally).
- **Micro Accuracy** — overall accuracy across all predictions.
- **Log Loss** — cross-entropy (lower is better).
- **Log Loss Reduction** — improvement over a naive baseline (closer to 1 is better).

---

## ✅ Scope & Limitations (read this)

**Works well:**
- Single **line** of text.
- Upper/lowercase letters and the supported punctuation.
- Cleanly-spaced glyphs in a trained-style font.

**Known limits:**
- **Single line only.** The segmenter projects ink across the whole image height, so multi-line images collapse into garbage. There is no line-detection stage.
- **Touching glyphs** (bold/tight fonts) are split heuristically by width. This recovers most characters but can confuse `m` with `nn` (one wide glyph vs. two narrow ones).
- **Inherent ambiguities** an image-only classifier can't resolve — e.g. lowercase `l` vs. capital `I` are pixel-identical in many fonts. These need a dictionary/language model to fix.
- **Not a document-OCR engine.** Screenshots/PDFs with multiple lines, proportional kerned text, underlines, mixed fonts, or characters outside the 63-class set are out of scope. For that, use **Tesseract** or a **vision LLM**.

This project is a from-scratch *learning* OCR, not a replacement for production OCR.

---

## 🐛 Troubleshooting

**`The type initializer for 'Tensorflow.Binding' threw an exception`**
The native TensorFlow runtime is missing or version-mismatched. This project pins **`SciSharp.TensorFlow.Redist` 2.3.1** (compatible with `TensorFlow.NET 0.20.1`) in the Console project; ensure it restored and that `tensorflow.dll` exists under `bin/.../runtimes/win-x64/native/`. Newer redist versions (2.10+) are **not** compatible and will reproduce this error.

**Model/data ends up in the wrong place (e.g. `C:\models\`, `C:\data\`)**
Paths resolve relative to the folder containing `OcrAlphabetTrainer.sln`. **Run commands from the `OcrAlphabetTrainer` directory.** Running from a parent folder (no `.sln` found) falls back to the drive root.

**`Training data not found`** — run `generate-data` first.

**Transcription drops/merges characters** — the input letters are touching (bold/tight). Use a trained-style font with normal spacing, or rely on the built-in over-wide-blob splitting. Lone punctuation in isolation is also ambiguous by design.

**Low accuracy** — generate more samples per character, ensure folder names are the encoded labels (`U_A`, `L_a`, `P_*`), and keep one character per training image.

---

## 📦 NuGet Dependencies

| Package | Version | Role |
|---------|---------|------|
| Microsoft.ML | 3.0.1 | ML.NET core |
| Microsoft.ML.ImageAnalytics | 3.0.1 | Image loading/transforms |
| Microsoft.ML.Vision | 3.0.1 | `ImageClassification` (ResNet) trainer |
| **SciSharp.TensorFlow.Redist** | **2.3.1** | Native TensorFlow runtime (required by ResNet) |
| SixLabors.ImageSharp | 3.1.6 | Image creation/processing |
| SixLabors.ImageSharp.Drawing | 2.1.5 | Text/glyph drawing |
| SixLabors.Fonts | 2.0.8 | Font rendering |

---

## 🚀 Possible Next Steps

- **Dictionary / language-model post-processing** — snap transcribed words to the nearest real words (fixes `l`↔`I`, `m`↔`nn`, etc.).
- **Digits `0–9`** as additional classes.
- **Line segmentation** (horizontal projection) to handle multiple lines.
- **More fonts / weights** (incl. bold) for robustness to real-world renderings.
- **Tesseract or a vision-LLM command** for true document OCR.

---

## 📚 Resources

- [ML.NET docs](https://learn.microsoft.com/en-us/dotnet/machine-learning/)
- [ML.NET image classification tutorial](https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/image-classification)
- [Transfer learning guide](https://learn.microsoft.com/en-us/dotnet/machine-learning/how-to-guides/how-to-use-transfer-learning)
- [OCR fundamentals (Wikipedia)](https://en.wikipedia.org/wiki/Optical_character_recognition)

---

**Happy Learning! 🎓** — questions? The source files are heavily commented; start with `TranscribeCommand.cs` and follow the pipeline.
