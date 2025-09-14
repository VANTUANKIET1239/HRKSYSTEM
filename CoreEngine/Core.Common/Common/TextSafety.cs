using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core.Common.Common
{
    public static class TextSafety
    {
        // Allow letters (incl. accents), digits, spaces and a conservative punctuation set.
        // Disallows angle brackets and backticks to avoid HTML/script injections.
        private static readonly Regex StrictAllowedRegex = new Regex(
            // No double spaces; only allowed chars
            @"\A(?!.*\s{2,})[\p{L}\p{M}\p{N}\p{Zs}\.\,\-_'’""\(\)\[\]\{\}:;!\?/@&\+#%]*\z",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Block obvious HTML tags like <script> or any <tag ...>
        private static readonly Regex HtmlTagRegex = new Regex(
            @"<\s*\/?\s*\w+[^>]*>",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        // Block "javascript:" URLs
        private static readonly Regex ScriptSchemeRegex = new Regex(
            @"(?i)javascript\s*:",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Block inline event handlers like onclick=, onload=, etc.
        private static readonly Regex HtmlOnEventRegex = new Regex(
            @"(?i)\bon\w+\s*=",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Very rough SQL-keyword screen (still use parameterized queries!)
        private static readonly Regex SqlKeywordRegex = new Regex(
            @"(?i)\b(SELECT|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|EXEC|UNION)\b|--|;",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Block path traversal
        private static readonly Regex PathTraversalRegex = new Regex(
            @"\.\./|\.\.\\",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Block control chars except CR/LF/TAB
        private static readonly Regex ControlCharsRegex = new Regex(
            @"[\p{C}--[\r\n\t]]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Returns true if the text looks "safe enough" for typical user-input fields.
        /// </summary>
        public static bool LooksSafe(string? input, int maxLen = 300)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (input.Length > maxLen) return false;

            // Normalize to avoid weird equivalent forms
            var s = input.Normalize(NormalizationForm.FormC);

            // Positive allow-list + negative blocks
            if (!StrictAllowedRegex.IsMatch(s)) return false;
            if (ControlCharsRegex.IsMatch(s)) return false;
            if (HtmlTagRegex.IsMatch(s)) return false;
            if (ScriptSchemeRegex.IsMatch(s)) return false;
            if (HtmlOnEventRegex.IsMatch(s)) return false;
            if (SqlKeywordRegex.IsMatch(s)) return false;
            if (PathTraversalRegex.IsMatch(s)) return false;

            return true;
        }
    }
}
