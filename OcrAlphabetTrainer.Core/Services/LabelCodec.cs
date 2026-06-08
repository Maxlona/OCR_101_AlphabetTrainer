namespace OcrAlphabetTrainer.Core.Services;

/// <summary>
/// Encodes/decodes character labels so every class maps to a valid, distinct on-disk folder name.
///
/// Two problems this solves:
/// 1. Windows filesystems are case-insensitive, so a folder named "A" and one named "a" are the
///    same directory — a raw single-character label would silently merge 'A' and 'a' into one class.
/// 2. Many punctuation characters (? : " * &lt; &gt; | / \) are illegal in file/folder names, and "."
///    is a reserved name. These must be mapped to safe alternatives.
///
/// Encoding: 'A' -> "U_A", 'a' -> "L_a", '.' -> "P_period", '!' -> "P_exclaim", etc.
/// </summary>
public static class LabelCodec
{
    // Punctuation -> safe folder name. Keep these names stable; they become the trained class names.
    private static readonly Dictionary<char, string> PunctuationToName = new()
    {
        ['.'] = "P_period",
        [','] = "P_comma",
        ['!'] = "P_exclaim",
        ['?'] = "P_question",
        [':'] = "P_colon",
        [';'] = "P_semicolon",
        ['\''] = "P_apostrophe",
        ['"'] = "P_quote",
        ['-'] = "P_hyphen",
        ['('] = "P_lparen",
        [')'] = "P_rparen",
    };

    private static readonly Dictionary<string, char> NameToPunctuation =
        PunctuationToName.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    /// <summary>Encodes a character into a case-safe, filename-safe label / folder name.</summary>
    public static string Encode(char c)
    {
        if (char.IsUpper(c)) return $"U_{c}";
        if (char.IsLower(c)) return $"L_{c}";
        if (PunctuationToName.TryGetValue(c, out var name)) return name;
        return c.ToString();
    }

    /// <summary>Decodes a label / folder name back into the displayable character string.</summary>
    public static string Decode(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return label;
        }

        if (label.Length == 3 && label[1] == '_' && (label[0] == 'U' || label[0] == 'L'))
        {
            return label[2].ToString();
        }

        if (NameToPunctuation.TryGetValue(label, out var c))
        {
            return c.ToString();
        }

        return label;
    }
}
