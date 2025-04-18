using System.Globalization;
using System.Text;

namespace E_Dukate.Application.Utilities;

public static class TextNormalizer
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        return text.Normalize(NormalizationForm.FormD)
                   .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                   .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
                   .ToString()
                   .ToLowerInvariant();
    }
}