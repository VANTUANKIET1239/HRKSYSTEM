using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Localization;

public sealed record TranslationBundle(string Group, string Culture, long Revision,
    IReadOnlyDictionary<string, string> Translations);
